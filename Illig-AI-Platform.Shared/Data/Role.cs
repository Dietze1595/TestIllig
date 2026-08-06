namespace Illig_AI_Platform.Shared.Data;

public class Role
{
    public int Id { get; set; }
    public string Name { get; set; } = "";

    public ICollection<UserProfileRole> UserProfileRoles { get; set; } = [];
}
