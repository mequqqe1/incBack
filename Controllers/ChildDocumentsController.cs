// Controllers/ChildDocumentsController.cs
using System.Text;
using INCBack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SharpAuthDemo.Contracts;
using SharpAuthDemo.Data;
using SharpAuthDemo.Services;

namespace SharpAuthDemo.Controllers;

[ApiController]
[Route("api/parent/children/{childId:guid}/documents")]
[Authorize(Roles = "Parent")]
public class ChildDocumentsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ProfileAccessService _access;
    private const long MaxBytes = 10 * 1024 * 1024; // 10MB

    public ChildDocumentsController(AppDbContext db, UserManager<ApplicationUser> userManager, ProfileAccessService access)
    { _db = db; _userManager = userManager; _access = access; }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ChildDocumentMeta>>> List(Guid childId)
    {
        var me = await _userManager.GetUserAsync(User);
        if (me is null) return Unauthorized();
        if (!await _access.CanAccessChildAsync(me.Id, childId)) return Forbid();

        var list = await _db.ChildDocuments.AsNoTracking()
            .Where(d => d.ChildId == childId)
            .OrderByDescending(d => d.CreatedAtUtc)
            .Select(d => new ChildDocumentMeta(d.Id, d.FileName, d.ContentType, d.SizeBytes, d.CreatedAtUtc, d.UploadedByUserId))
            .ToListAsync();

        return list;
    }

    [HttpPost]
    public async Task<ActionResult<ChildDocumentMeta>> Upload(Guid childId, UploadChildDocumentRequest req)
    {
        var me = await _userManager.GetUserAsync(User);
        if (me is null) return Unauthorized();
        if (!await _access.CanAccessChildAsync(me.Id, childId)) return Forbid();

        var childExists = await _db.Children.AnyAsync(c => c.Id == childId);
        if (!childExists) return NotFound();

        if (string.IsNullOrWhiteSpace(req.FileName)) return BadRequest(new { error = "FileName required" });
        if (string.IsNullOrWhiteSpace(req.ContentBase64)) return BadRequest(new { error = "ContentBase64 required" });

        byte[] bytes;
        try { bytes = Convert.FromBase64String(req.ContentBase64); }
        catch { return BadRequest(new { error = "Invalid base64" }); }

        if (bytes.Length == 0) return BadRequest(new { error = "Empty file" });
        if (bytes.Length > MaxBytes) return BadRequest(new { error = "File too large (max 10MB)" });

        // проверка PDF
        var header = Encoding.ASCII.GetString(bytes.Take(4).ToArray());
        if (header != "%PDF") return BadRequest(new { error = "Only PDF is allowed" });

        var entity = new ChildDocument
        {
            ChildId = childId,
            FileName = req.FileName,
            ContentType = "application/pdf",
            SizeBytes = req.SizeBytes ?? bytes.Length,
            ContentBase64 = req.ContentBase64,
            UploadedByUserId = me.Id,
        };

        _db.ChildDocuments.Add(entity);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(Download), new { childId, docId = entity.Id },
            new ChildDocumentMeta(entity.Id, entity.FileName, entity.ContentType, entity.SizeBytes, entity.CreatedAtUtc, entity.UploadedByUserId));
    }

    [HttpGet("{docId:guid}")]
    public async Task<IActionResult> Download(Guid childId, Guid docId)
    {
        var me = await _userManager.GetUserAsync(User);
        if (me is null) return Unauthorized();
        if (!await _access.CanAccessChildAsync(me.Id, childId)) return Forbid();

        var doc = await _db.ChildDocuments.FirstOrDefaultAsync(d => d.Id == docId && d.ChildId == childId);
        if (doc is null) return NotFound();

        var bytes = Convert.FromBase64String(doc.ContentBase64);
        return File(bytes, doc.ContentType, doc.FileName);
    }

    [HttpDelete("{docId:guid}")]
    public async Task<IActionResult> Delete(Guid childId, Guid docId)
    {
        var me = await _userManager.GetUserAsync(User);
        if (me is null) return Unauthorized();
        if (!await _access.CanAccessChildAsync(me.Id, childId)) return Forbid();

        var doc = await _db.ChildDocuments.FirstOrDefaultAsync(d => d.Id == docId && d.ChildId == childId);
        if (doc is null) return NotFound();

        _db.ChildDocuments.Remove(doc);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
