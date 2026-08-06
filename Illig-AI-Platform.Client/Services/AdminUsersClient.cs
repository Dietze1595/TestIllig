using System.Net;
using System.Net.Http.Json;
using Illig_AI_Platform.Client.Models.Admin;

namespace Illig_AI_Platform.Client.Services;

/// <summary>
/// Client für die Admin-Benutzerverwaltung (api/v1/admin/users). Nicht-2xx-Antworten werden vom
/// <see cref="ApiErrorHandler"/> bereits in eine <see cref="ApiRequestException"/> übersetzt.
/// </summary>
public sealed class AdminUsersClient(HttpClient http)
{
    public async Task<IReadOnlyList<AdminUserDto>> ListeAsync() =>
        await http.GetFromJsonAsync<List<AdminUserDto>>("api/v1/admin/users") ?? [];

    public async Task<IReadOnlyList<AdminRoleDto>> RollenAsync() =>
        await http.GetFromJsonAsync<List<AdminRoleDto>>("api/v1/admin/users/roles") ?? [];

    public async Task RollenSpeichernAsync(Guid userId, IEnumerable<int> roleIds)
    {
        var response = await http.PutAsJsonAsync(
            $"api/v1/admin/users/{userId}/roles",
            new UpdateUserRolesRequest { RoleIds = roleIds.ToList() });

        // 400/401/403/5xx hat der ApiErrorHandler bereits als ApiRequestException geworfen. 404 lässt er
        // durch (Clients werten es selbst aus) — hier praktisch nur bei zwischenzeitlich gelöschtem Nutzer.
        if (response.StatusCode == HttpStatusCode.NotFound)
            throw new ApiRequestException(404, "Benutzer nicht gefunden. Bitte lade die Liste neu.");
    }
}
