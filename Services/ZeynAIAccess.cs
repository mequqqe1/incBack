// Services/ZeynAIAccess.cs
using INCBack.Models;
using Microsoft.EntityFrameworkCore;
using SharpAuthDemo.Data;

public interface IZeynAIAccess
{
    Task<bool> CanAccessChildAsync(string userId, Guid childId, CancellationToken ct);
}

public class ZeynAIAccess : IZeynAIAccess
{
    private readonly AppDbContext _db;
    public ZeynAIAccess(AppDbContext db) { _db = db; }

    public async Task<bool> CanAccessChildAsync(string userId, Guid childId, CancellationToken ct)
    {
        // владелец ребёнка
        var isOwner = await _db.Children
            .AnyAsync(c => c.Id == childId && c.ParentProfile!.UserId == userId, ct);
        if (isOwner) return true;

        // принятый член семьи (если используете CaregiverMember.Status == Accepted)
        var isCaregiver = await _db.Children
            .Where(c => c.Id == childId)
            .Join(_db.CaregiverMembers,
                c => c.ParentProfileId,
                m => m.ParentProfileId,
                (c, m) => new { m.UserId, m.Status })
            .AnyAsync(x => x.UserId == userId && x.Status == CaregiverStatus.Active, ct);

        return isCaregiver;
    }
}