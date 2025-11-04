// Services/ProfileAccessService.cs
using Microsoft.EntityFrameworkCore;
using SharpAuthDemo.Data;

namespace SharpAuthDemo.Services;

public class ProfileAccessService
{
    private readonly AppDbContext _db;
    public ProfileAccessService(AppDbContext db) { _db = db; }

    public async Task<List<Guid>> GetAccessibleProfileIdsAsync(string userId)
    {
        var owned = _db.ParentProfiles
            .Where(p => p.UserId == userId)
            .Select(p => p.Id);

        var member = _db.CaregiverMembers
            .Where(m => m.UserId == userId && m.Status == INCBack.Models.CaregiverStatus.Active)
            .Select(m => m.ParentProfileId);

        return await owned.Union(member).Distinct().ToListAsync();
    }

    public async Task<bool> CanAccessChildAsync(string userId, Guid childId)
    {
        var q =
            from c in _db.Children
            where c.Id == childId
            join p in _db.ParentProfiles on c.ParentProfileId equals p.Id
            select new { p.Id, p.UserId };

        var record = await q.FirstOrDefaultAsync();
        if (record is null) return false;

        if (record.UserId == userId) return true;

        var member = await _db.CaregiverMembers
            .AnyAsync(m => m.ParentProfileId == record.Id &&
                           m.UserId == userId &&
                           m.Status == INCBack.Models.CaregiverStatus.Active);
        return member;
    }
}