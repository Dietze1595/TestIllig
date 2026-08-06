using Asp.Versioning;
using Illig_AI_Platform.Services;
using Illig_AI_Platform.Shared.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Illig_AI_Platform.Controllers
{
    /// <summary>
    /// Admin-only Benutzerverwaltung: alle Nutzer auflisten und deren App-Rollen setzen.
    /// Bewusst getrennt vom Self-Service-<see cref="UsersController"/> (api/v1/users), damit die
    /// Autorisierungsgrenze eindeutig ist: hier bearbeitet ein Admin *andere* Nutzer.
    /// </summary>
    [ApiController]
    [ApiVersion(1.0)]
    [Route("api/v{version:apiVersion}/admin/users")]
    [ApiExplorerSettings(GroupName = "app")]
    [Tags("AdminUsers")]
    [Authorize(Roles = AppRoles.Admin)]
    public class AdminUsersController(
        AppDbContext dbContext, IMemoryCache cache, ILogger<AdminUsersController> logger) : ControllerBase
    {
        // Rollen kommen aus der kanonischen Seed-Liste (Single Source of Truth), nicht aus der DB —
        // so hängen Anzeige und Validierung nicht von seed-abhängigen Role-Zeilen ab.
        private static readonly IReadOnlyDictionary<int, string> RoleNameById =
            AppRoles.Seed.ToDictionary(r => r.Id, r => r.Name);

        private const string NovazoonEmailDomain = "novazoon.de";

        private static readonly IReadOnlySet<int> PrivilegedRoleIds =
            AppRoles.Seed
                .Where(r => r.Name is AppRoles.Admin or AppRoles.Dev)
                .Select(r => r.Id)
                .ToHashSet();

        /// <summary>
        /// Alle Nutzer inkl. ihrer aktuellen Rollen — <b>eine Zeile je Nutzer-GUID</b>. Es wird strikt
        /// nach der Profil-Id (GUID) gruppiert, damit ein Nutzer mit mehreren Rollen genau einmal
        /// erscheint (alle Rollen in der Mehrfachauswahl), statt pro Rolle mehrfach.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(IReadOnlyList<AdminUserResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<IReadOnlyList<AdminUserResponse>>> GetUsersAsync()
        {
            var profile = await dbContext.UserProfiles
                .AsNoTracking()
                .Select(u => new
                {
                    u.Id,
                    u.Email,
                    u.FullName,
                    u.DisplayName,
                    RoleIds = u.UserProfileRoles.Select(upr => upr.RoleId).ToList()
                })
                .ToListAsync();

            // Strikt nach GUID gruppieren: robust auch dann, wenn die Datenquelle mehrere Zeilen je
            // Nutzer liefern würde — pro GUID bleibt genau ein Eintrag mit vereinten Rollen.
            var response = profile
                .GroupBy(u => u.Id)
                .Select(gruppe =>
                {
                    var erster = gruppe.First();
                    var rollen = gruppe
                        .SelectMany(u => u.RoleIds)
                        .Distinct()
                        .Where(RoleNameById.ContainsKey)
                        .Select(id => RoleNameById[id])
                        .ToList();

                    return new AdminUserResponse(
                        erster.Id, erster.Email, erster.FullName, erster.DisplayName, rollen);
                })
                .OrderBy(u => u.FullName)
                .ThenBy(u => u.Email)
                .ToList();

            return Ok(response);
        }

        /// <summary>Die zuweisbaren App-Rollen — Server als Single Source of Truth für die UI.</summary>
        [HttpGet("roles")]
        [ProducesResponseType(typeof(IReadOnlyList<AdminRoleResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public ActionResult<IReadOnlyList<AdminRoleResponse>> GetRoles() =>
            Ok(AppRoles.Seed.Select(r => new AdminRoleResponse(r.Id, r.Name)).ToList());

        /// <summary>Ersetzt die Rollen eines Nutzers (Set-Semantik), adressiert über seine GUID.</summary>
        [HttpPut("{id:guid}/roles")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateRolesAsync(Guid id, [FromBody] UpdateUserRolesRequest request)
        {
            if (request is null)
                return BadRequest("Keine Daten übergeben.");

            var requestedIds = (request.RoleIds ?? []).Distinct().ToList();

            var unbekannt = requestedIds.Where(rid => !RoleNameById.ContainsKey(rid)).ToList();
            if (unbekannt.Count > 0)
                return BadRequest($"Unbekannte Rollen-Id(s): {string.Join(", ", unbekannt)}.");

            var user = await dbContext.UserProfiles
                .Include(u => u.UserProfileRoles)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user is null)
                return NotFound("Benutzerprofil nicht gefunden.");

            if (IstNovazoon(user.Email) && !IstNovazoon(User.GetEmail()))
            {
                logger.LogWarning(
                    "Nutzer {AdminId} versuchte die Rollen des Novazoon-Nutzers {UserId} zu ändern — abgelehnt.",
                    User.GetUserId(), id);
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    "Nur Novazoon-Mitarbeitende dürfen die Rollen von Novazoon-Nutzern ändern.");
            }

            // Selbstschutz: Admins und Entwickler dürfen sich die eigene privilegierte Rolle nicht
            // entziehen — sonst könnten sie sich versehentlich selbst aussperren.
            var selbstEntzogenePrivilegien = user.UserProfileRoles
                .Where(upr => PrivilegedRoleIds.Contains(upr.RoleId))
                .Any(upr => !requestedIds.Contains(upr.RoleId));
            if (User.GetUserId() == id && selbstEntzogenePrivilegien)
                return BadRequest("Die eigene Admin- oder DEV-Rolle kann nicht entzogen werden.");

            // Nur die Differenz anwenden: bereits vorhandene Join-Zeilen unangetastet lassen, damit EF
            // nicht dieselbe (UserProfileId, RoleId) im selben SaveChanges löscht und neu einfügt.
            var vorhandeneIds = user.UserProfileRoles.Select(upr => upr.RoleId).ToHashSet();
            var zuEntfernen = user.UserProfileRoles.Where(upr => !requestedIds.Contains(upr.RoleId)).ToList();
            var zuErgaenzen = requestedIds.Where(rid => !vorhandeneIds.Contains(rid)).ToList();

            foreach (var upr in zuEntfernen)
                user.UserProfileRoles.Remove(upr);
            foreach (var rid in zuErgaenzen)
                user.UserProfileRoles.Add(new UserProfileRole { UserProfileId = id, RoleId = rid });

            await dbContext.SaveChangesAsync();

            // Rollen-Cache des Zielnutzers invalidieren, damit die Änderung beim nächsten Request greift
            // (statt erst nach Ablauf des 5-Minuten-TTL in RoleClaimsTransformation).
            cache.Remove(RoleClaimsTransformation.RoleCacheKey(id));

            logger.LogInformation(
                "Admin {AdminId} setzte Rollen von Nutzer {UserId} auf [{Rollen}].",
                User.GetUserId(), id, string.Join(", ", requestedIds.Select(r => RoleNameById[r])));

            return Ok();
        }

        private static bool IstNovazoon(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;
            var at = email.LastIndexOf('@');
            if (at < 0 || at == email.Length - 1)
                return false;
            return string.Equals(email[(at + 1)..].Trim(), NovazoonEmailDomain, StringComparison.OrdinalIgnoreCase);
        }

        public record AdminUserResponse(
            Guid Id, string Email, string FullName, string DisplayName, List<string> Roles);

        public record AdminRoleResponse(int Id, string Name);

        public record UpdateUserRolesRequest(List<int>? RoleIds);
    }
}
