namespace Illig_AI_Platform.Shared.Data;

public class UserProfileRole
{
    public Guid UserProfileId { get; set; }
    public UserProfile UserProfile { get; set; } = null!;

    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;
}
