namespace Illig_AI_Platform.Shared.Data;

public class UserProfile
{
    public Guid Id { get; set; }
    public string ObjectId { get; set; } = "";
    public string Email { get; set; } = "";
    public string FullName { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Name { get; set; } = "";
    public string Vorname { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string? ProfileImageBase64 { get; set; }
    public string? ProfileImageType { get; set; }

    public string? PreferredLanguage { get; set; }
    public string? Address { get; set; }
    public string? Street { get; set; }
    public string? PostalCode { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    public ICollection<UserProfileRole> UserProfileRoles { get; set; } = [];
}
