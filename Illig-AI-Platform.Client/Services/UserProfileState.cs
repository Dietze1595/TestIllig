using System.Net.Http.Headers;
using System.Net.Http.Json;
using Illig_AI_Platform.Client.Models;
using Microsoft.AspNetCore.Components.Forms;

namespace Illig_AI_Platform.Client.Services;

/// <summary>
/// Hält das aus der DB geladene UserProfile zentral vor, damit Header und Profilseite
/// dieselbe Quelle nutzen. Nach Änderungen (Name/Bild) wird neu geladen und über
/// <see cref="Changed"/> benachrichtigt, sodass z. B. der Header sofort aktualisiert.
/// </summary>
public class UserProfileState(HttpClient http)
{
    private const long MaxImageBytes = 4 * 1024 * 1024; // 4 MB

    public UserProfileDto? Profile { get; private set; }
    public bool Loaded { get; private set; }
    public bool HatAdminBerechtigungen =>
        AppClientRoles.HatAdminBerechtigungen(Profile?.Roles ?? []);

    public event Action? Changed;

    public async Task EnsureLoadedAsync()
    {
        if (Loaded) return;
        await ReloadAsync();
    }

    /// <summary>
    /// Prüft (nach dem Laden des Profils), ob der Nutzer mindestens eine der angegebenen
    /// App-Rollen besitzt. App-Rollen kommen aus der DB (über api/v1/users/me), nicht aus dem B2C-Token.
    /// </summary>
    public async Task<bool> HasAnyRoleAsync(params string[] roles)
    {
        await EnsureLoadedAsync();
        return HatAdminBerechtigungen || Profile?.Roles.Any(roles.Contains) == true;
    }

    public async Task ReloadAsync()
    {
        try
        {
            Profile = await http.GetFromJsonAsync<UserProfileDto>("api/v1/users/me");
        }
        catch
        {
            // Best effort — Profile bleibt ggf. null, Header fällt dann auf Claims zurück.
        }
        finally
        {
            Loaded = true;
            Changed?.Invoke();
        }
    }

    public async Task<bool> SaveDisplayNameAsync(string displayName)
    {
        try
        {
            var response = await http.PatchAsJsonAsync(
                "api/v1/users/displayname", new { DisplayName = displayName });

            if (!response.IsSuccessStatusCode)
                return false;
        }
        catch (ApiRequestException)
        {
            return false;
        }

        await ReloadAsync();
        return true;
    }

    public async Task<bool> UploadPhotoAsync(IBrowserFile file)
    {
        using var content = new MultipartFormDataContent();
        using var stream = file.OpenReadStream(MaxImageBytes);
        using var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
        // Feldname muss zum Controller-Parameter [FromForm] IFormFile browserFile passen.
        content.Add(fileContent, "browserFile", file.Name);

        try
        {
            var response = await http.PostAsync("api/v1/users/icon", content);
            if (!response.IsSuccessStatusCode)
                return false;
        }
        catch (ApiRequestException)
        {
            return false;
        }

        await ReloadAsync();
        return true;
    }

    public async Task<bool> UploadPhotoAsync(byte[] imageBytes, string contentType, string fileName)
    {
        if (imageBytes.Length == 0 || imageBytes.Length > MaxImageBytes)
            return false;

        using var content = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(imageBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(fileContent, "browserFile", fileName);

        try
        {
            var response = await http.PostAsync("api/v1/users/icon", content);
            if (!response.IsSuccessStatusCode)
                return false;
        }
        catch (ApiRequestException)
        {
            return false;
        }

        await ReloadAsync();
        return true;
    }
}
