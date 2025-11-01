using Microsoft.AspNetCore.Mvc;
using SharpAuthDemo.Data;
using SharpAuthDemo.Models;

namespace SharpAuthDemo.Controllers;

[ApiController]
[Route("api/lookups")]
public class LookupController : ControllerBase
{
    private readonly AppDbContext _db;
    public LookupController(AppDbContext db) => _db = db;

    [HttpGet("specializations")]
    public ActionResult<IEnumerable<LookupItem>> GetSpecializations()
        => Ok(_db.Specializations
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
            .Select(x => new LookupItem(x.Id, x.Name, x.Description, x.SortOrder, x.IsActive))
            .ToList());

    [HttpGet("skills")]
    public ActionResult<IEnumerable<LookupItem>> GetSkills()
        => Ok(_db.Skills
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
            .Select(x => new LookupItem(x.Id, x.Name, x.Description, x.SortOrder, x.IsActive))
            .ToList());
}