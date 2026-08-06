using Illig_AI_Platform.Shared.Data;
using Microsoft.EntityFrameworkCore;

namespace Illig_AI_Platform.Shared.Services;

public class UserProfileService(AppDbContext db)
{
    public async Task EnsureProfileCreatedAsync(
        Guid id, string email, string displayName, string name, string vorname)
    {
        var exists = await db.UserProfiles.AnyAsync(p => p.Id == id);
        if (exists) return;

        db.UserProfiles.Add(new UserProfile
        {
            Id = id,
            ObjectId = id.ToString(),
            Email = email,
            DisplayName = displayName,
            FullName = $"{vorname} {name}".Trim(),
            Name = name,
            Vorname = vorname,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }
}
