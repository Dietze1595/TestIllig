using Asp.Versioning;
using Illig_AI_Platform.Services;
using Illig_AI_Platform.Shared.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Illig_AI_Platform.Controllers
{
    [ApiController]
    [ApiVersion(1.0)]
    [Route("api/v{version:apiVersion}/users")]
    [ApiExplorerSettings(GroupName = "app")]
    [Tags("Users")]
    [Authorize]
    public class UsersController(AppDbContext dbContext, ILogger<UsersController> logger) : ControllerBase
    {

        [HttpGet("me")]
        [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<UserProfileResponse>> GetAsync()
        {
            if (User.GetUserId() is not Guid userId)
            {
                return NotFound("User ID could not be extracted from context.");
            }

            var profile = await dbContext.UserProfiles
                .Include(p => p.UserProfileRoles)
                .ThenInclude(upr => upr.Role)
                .FirstOrDefaultAsync(p => p.Id == userId);

            if (profile is null)
            {
                return NotFound("User profile not found.");
            }

            var image = !string.IsNullOrEmpty(profile.ProfileImageBase64) && !string.IsNullOrEmpty(profile.ProfileImageType)
                ? $"data:{profile.ProfileImageType};base64,{profile.ProfileImageBase64}"
                : profile.ProfileImageBase64;

            var roles = profile.UserProfileRoles.Select(upr => upr.Role.Name).ToList();

            return Ok(new UserProfileResponse(
                profile.Id, profile.Email, profile.FullName, profile.DisplayName,
                image, profile.ProfileImageType, roles));
        }


        private const long MaxImageBytes = 4 * 1024 * 1024;

        [HttpPost("icon")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UploadProfileImage([FromForm] IFormFile browserFile)
        {
            if (browserFile is null || browserFile.Length == 0)
                return BadRequest("No file uploaded.");

            if (browserFile.Length > MaxImageBytes)
                return BadRequest("Image too large (max 4 MB).");

            if (!browserFile.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                return BadRequest("Only image files are allowed.");

            if (User.GetUserId() is not Guid userId)
            {
                return Unauthorized();
            }

            try
            {
                using var memoryStream = new MemoryStream();
                await browserFile.CopyToAsync(memoryStream);
                var imageBytes = memoryStream.ToArray();
                var base64String = Convert.ToBase64String(imageBytes);

                var userProfile = await dbContext.UserProfiles.FindAsync(userId);
                if (userProfile is null)
                {
                    return NotFound("User profile not found.");
                }

                userProfile.ProfileImageBase64 = base64String;
                userProfile.ProfileImageType = browserFile.ContentType;
                dbContext.Update(userProfile);
                await dbContext.SaveChangesAsync();

                return Ok();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error uploading profile image for user {UserId}.", userId);
                return StatusCode(500, "Internal server error while uploading profile image.");
            }
        }

        [HttpPatch("displayname")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateDisplayNameAsync([FromBody] DisplayNameUpdate request)
        {
            var displayName = request?.DisplayName?.Trim();
            if (string.IsNullOrWhiteSpace(displayName))
                return BadRequest("Display name must not be empty.");

            if (User.GetUserId() is not Guid userId)
                return Unauthorized();

            var dbo = await dbContext.UserProfiles.FindAsync(userId);
            if (dbo is null)
                return NotFound("User profile not found.");

            dbo.DisplayName = displayName;
            await dbContext.SaveChangesAsync();

            logger.LogInformation("Display name updated for user {UserId}.", userId);
            return Ok();
        }

        [HttpPatch]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(string), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateUserAsync([FromBody] UserProfileUpdateRequest profile)
        {
            if (profile is null)
            {
                return BadRequest("No user profile provided.");
            }

            var maybeUserId = User.GetUserId();
            if (maybeUserId is not Guid userId || userId != profile.Id)
            {
                logger.LogWarning("Unauthorized update attempt by {UserId} (payload: {PayloadId}).", maybeUserId, profile.Id);
                return StatusCode(StatusCodes.Status403Forbidden, "Not authorized.");
            }

            try
            {
                bool result = false;
                var dbo = await dbContext.UserProfiles.FindAsync(profile.Id);
                if (dbo != null)
                {
                    dbo.PreferredLanguage = profile.PreferredLanguage;
                    dbo.Address = profile.Address;
                    dbo.Street = profile.Street;
                    dbo.Country = profile.Country;
                    dbo.PostalCode = profile.PostalCode;
                    dbo.City = profile.City;
                    dbo.Longitude = profile.Longitude;
                    dbo.Latitude = profile.Latitude;
                    dbContext.Update(dbo);
                    result = await dbContext.SaveChangesAsync() > 0;
                }

                if (result)
                {
                    logger.LogInformation("User profile {UserId} update successfully.", userId);
                    return Ok("User profile updated.");
                }
                else
                {
                    logger.LogWarning("Failed to update user profile for user {UserId}.", userId);
                    return BadRequest("Failed to update user profile.");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while updating preferred language for user {UserId}.", userId);
                return StatusCode(500, "Internal server error while updating preferred language.");
            }
        }

        public record DisplayNameUpdate(string? DisplayName);

        /// <summary>
        /// Nur die vom Nutzer selbst editierbaren Profilfelder — bewusst keine EF-Entity als
        /// Request-DTO, damit ein Client nicht versehentlich (oder böswillig) Email, ObjectId,
        /// Rollen o.ä. mitschicken und überschreiben kann.
        /// </summary>
        public record UserProfileUpdateRequest(
            Guid Id,
            string? PreferredLanguage,
            string? Address,
            string? Street,
            string? Country,
            string? PostalCode,
            string? City,
            double? Longitude,
            double? Latitude);

        public record UserProfileResponse(
            Guid Id,
            string Email,
            string FullName,
            string DisplayName,
            string? ProfileImageBase64,
            string? ProfileImageType,
            List<string> Roles);
    }
}
