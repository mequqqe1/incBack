using INCBack.Models;
using Microsoft.EntityFrameworkCore;
using SharpAuthDemo.Data;

namespace SharpAuthDemo.Services;

/// <summary>Роль текущего пользователя в семье.</summary>
public enum FamilyRole { Owner, Admin, Caregiver }

/// <summary>Контекст текущей семьи: профиль, владелец, роль пользователя.</summary>
public record FamilyContext(Guid ParentProfileId, string OwnerUserId, FamilyRole Role);

public interface IFamilyContextService
{
    /// <summary>Возвращает контекст семьи для пользователя: как владелец или как принятый участник. Если несколько семей — первая.</summary>
    Task<FamilyContext?> GetCurrentFamilyAsync(string userId, CancellationToken ct = default);

    /// <summary>Может ли пользователь управлять участниками (приглашать, удалять, менять роль).</summary>
    Task<bool> CanManageMembersAsync(string userId, CancellationToken ct = default);
}

public class FamilyContextService : IFamilyContextService
{
    private readonly AppDbContext _db;

    public FamilyContextService(AppDbContext db) => _db = db;

    public async Task<FamilyContext?> GetCurrentFamilyAsync(string userId, CancellationToken ct = default)
    {
        // Владелец профиля
        var owned = await _db.ParentProfiles
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .Select(p => new FamilyContext(p.Id, p.UserId, FamilyRole.Owner))
            .FirstOrDefaultAsync(ct);
        if (owned != null) return owned;

        // Участник семьи (Accepted или Active)
        var member = await _db.CaregiverMembers
            .AsNoTracking()
            .Where(m => m.UserId == userId && (m.Status == CaregiverStatus.Active || m.Status == CaregiverStatus.Accepted))
            .Join(_db.ParentProfiles, m => m.ParentProfileId, p => p.Id, (m, p) => new { m.ParentProfileId, m.IsAdmin, OwnerUserId = p.UserId })
            .FirstOrDefaultAsync(ct);
        if (member == null) return null;

        var role = member.IsAdmin ? FamilyRole.Admin : FamilyRole.Caregiver;
        return new FamilyContext(member.ParentProfileId, member.OwnerUserId, role);
    }

    public async Task<bool> CanManageMembersAsync(string userId, CancellationToken ct = default)
    {
        var ctx = await GetCurrentFamilyAsync(userId, ct);
        return ctx is { Role: FamilyRole.Owner or FamilyRole.Admin };
    }
}
