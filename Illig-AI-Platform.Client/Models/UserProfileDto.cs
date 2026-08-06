namespace Illig_AI_Platform.Client.Models;

/// <summary>
/// Leichtes Abbild des Server-UserProfile für den Client (ohne EF/Shared-Abhängigkeit).
/// Wird aus GET api/users/me deserialisiert. ProfileImageBase64 enthält bereits eine
/// fertige Data-URI (der Server formatiert sie beim Laden).
/// </summary>
public class UserProfileDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = "";
    public string FullName { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string? ProfileImageBase64 { get; set; }
    public string? ProfileImageType { get; set; }
    public List<string> Roles { get; set; } = [];
}
