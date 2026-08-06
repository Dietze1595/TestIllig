namespace Illig_AI_Platform.Client.Models.Admin;

/// <summary>
/// Ein Nutzer (eine Zeile je GUID) in der Admin-Benutzerverwaltung inkl. seiner aktuell zugewiesenen
/// App-Rollen (Rollennamen). Deserialisiert aus GET api/v1/admin/users.
/// </summary>
public class AdminUserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = "";
    public string FullName { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public List<string> Roles { get; set; } = [];
}

/// <summary>Eine zuweisbare App-Rolle. Deserialisiert aus GET api/v1/admin/users/roles.</summary>
public class AdminRoleDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

/// <summary>Set-Semantik: die übergebenen Rollen-Ids ersetzen die bisherigen Rollen des Nutzers.</summary>
public class UpdateUserRolesRequest
{
    public List<int> RoleIds { get; set; } = [];
}
