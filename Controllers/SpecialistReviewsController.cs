// Controllers/SpecialistReviewsController.cs
using System.ComponentModel.DataAnnotations;
using INCBack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SharpAuthDemo.Data;
using SharpAuthDemo.Models;
using Microsoft.AspNetCore.Identity;

namespace INCBack.Controllers;

[ApiController]
[Route("api/specialists/{specialistUserId}/reviews")]
public class SpecialistReviewsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly IConfiguration _cfg;

    // если true — отзыв можно оставить только после завершённого визита
    private bool RequireCompletedBooking => 
        _cfg.GetValue("Reviews:RequireCompletedBooking", false);

    // анти-спам для «открытых» отзывов: min N минут между отзывами одного пользователя к одному спецу
    private int OpenReviewCooldownMinutes => 
        _cfg.GetValue("Reviews:OpenReviewCooldownMinutes", 10);

    public SpecialistReviewsController(AppDbContext db, UserManager<ApplicationUser> users, IConfiguration cfg)
    {
        _db = db; _users = users; _cfg = cfg;
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // DTO
    public record ReviewCreateDto(Guid? BookingId, [Range(1,5)] int Rating, string? Comment, bool IsAnonymous = false);
    public record ReviewVm(Guid Id, int Rating, string? Comment, string AuthorName, DateTime CreatedAtUtc);
    public record ReviewsSummaryVm(double average, int count, IReadOnlyList<ReviewVm> items);

    // ─────────────────────────────────────────────────────────────────────────────
    // GET: список + агрегаты (пагинация)
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<ReviewsSummaryVm>> Get(
        string specialistUserId,
        int skip = 0,
        int take = 20)
    {
        take = Math.Clamp(take, 1, 50);

        var baseQ = _db.SpecialistReviews
            .AsNoTracking()
            .Where(r => r.SpecialistUserId == specialistUserId && r.IsVisible);

        var avg = await baseQ.Select(r => (double?)r.Rating).AverageAsync() ?? 0;
        var cnt = await baseQ.CountAsync();

        var items = await baseQ
            .OrderByDescending(r => r.CreatedAtUtc)
            .Skip(skip).Take(take)
            .Select(r => new ReviewVm(
                r.Id,
                r.Rating,
                r.Comment,
                r.IsAnonymous
                    ? "Аноним"
                    : _db.Users.Where(u => u.Id == r.ParentUserId)
                               .Select(u => u.UserName!)
                               .FirstOrDefault() ?? "Пользователь",
                r.CreatedAtUtc))
            .ToListAsync();

        return Ok(new ReviewsSummaryVm(Math.Round(avg, 2), cnt, items));
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // POST: создать отзыв
    // Роль можешь сузить до родителя: [Authorize(Roles = "Parent")]
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Post(string specialistUserId, [FromBody] ReviewCreateDto dto)
    {
        var parentUserId = _users.GetUserId(User)!;

        // Проверим, что специалист существует (по профилю)
        var specProfile = await _db.SpecialistProfiles.AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == specialistUserId);
        if (specProfile is null) return NotFound("Специалист не найден.");

        Booking? booking = null;

        if (RequireCompletedBooking)
        {
            // Жёсткий режим: отзыв только по завершённому визиту
            if (dto.BookingId is null) return BadRequest("Нужен идентификатор завершённого визита.");

            booking = await _db.Bookings.FirstOrDefaultAsync(b =>
                b.Id == dto.BookingId &&
                b.SpecialistUserId == specialistUserId &&
                b.ParentUserId == parentUserId);

            if (booking is null) return Forbid();
            if (booking.Status != BookingStatus.Completed)
                return BadRequest("Отзыв возможен только после завершения визита.");

            // один отзыв на бронирование
            var alreadyForBooking = await _db.SpecialistReviews.AnyAsync(r => r.BookingId == booking.Id);
            if (alreadyForBooking) return Conflict("Для этого визита отзыв уже существует.");
        }
        else
        {
            // Открытые отзывы: бронирование не обязательно
            // но если передали — валидируем соответствие пользователю и спецу (и не даём дубли)
            if (dto.BookingId is Guid bkId)
            {
                booking = await _db.Bookings.FirstOrDefaultAsync(b =>
                    b.Id == bkId &&
                    b.SpecialistUserId == specialistUserId &&
                    b.ParentUserId == parentUserId);
                if (booking is null) return Forbid();

                var alreadyForBooking = await _db.SpecialistReviews.AnyAsync(r => r.BookingId == bkId);
                if (alreadyForBooking) return Conflict("Для этого визита отзыв уже существует.");
            }

            // Анти-спам: не чаще, чем раз в N минут для одного спеца
            var cooldownFrom = DateTime.UtcNow.AddMinutes(-OpenReviewCooldownMinutes);
            var recent = await _db.SpecialistReviews.AnyAsync(r =>
                r.SpecialistUserId == specialistUserId &&
                r.ParentUserId == parentUserId &&
                r.CreatedAtUtc >= cooldownFrom);
            if (recent)
                return StatusCode(StatusCodes.Status429TooManyRequests,
                    $"Слишком часто. Попробуйте позже (через {OpenReviewCooldownMinutes} мин).");
        }

        var review = new SpecialistReview
        {
            SpecialistUserId = specialistUserId,
            ParentUserId = parentUserId,
            BookingId = dto.BookingId,
            Rating = dto.Rating,
            Comment = string.IsNullOrWhiteSpace(dto.Comment) ? null : dto.Comment.Trim(),
            IsAnonymous = dto.IsAnonymous,
            CreatedAtUtc = DateTime.UtcNow,
            IsVisible = true
        };

        using var tx = await _db.Database.BeginTransactionAsync();
        _db.SpecialistReviews.Add(review);
        await _db.SaveChangesAsync();

        await RecomputeAggregates(specialistUserId);
        await tx.CommitAsync();

        return CreatedAtAction(nameof(Get), new { specialistUserId }, new { review.Id });
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Скрыть/показать отзыв (модерация)
    [HttpPatch("{id:guid}/visibility")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SetVisibility(string specialistUserId, Guid id, [FromQuery] bool visible = true)
    {
        var r = await _db.SpecialistReviews.FirstOrDefaultAsync(x => x.Id == id && x.SpecialistUserId == specialistUserId);
        if (r is null) return NotFound();

        r.IsVisible = visible;
        await _db.SaveChangesAsync();
        await RecomputeAggregates(specialistUserId);
        return NoContent();
    }

    // Удаление (админ)
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(string specialistUserId, Guid id)
    {
        var r = await _db.SpecialistReviews.FirstOrDefaultAsync(x => x.Id == id && x.SpecialistUserId == specialistUserId);
        if (r is null) return NotFound();

        _db.SpecialistReviews.Remove(r);
        await _db.SaveChangesAsync();
        await RecomputeAggregates(specialistUserId);
        return NoContent();
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Пересчёт агрегатов (денормализация в профиле спеца)
    private async Task RecomputeAggregates(string specialistUserId)
    {
        var agg = await _db.SpecialistReviews
            .Where(x => x.SpecialistUserId == specialistUserId && x.IsVisible)
            .GroupBy(_ => 1)
            .Select(g => new { Avg = g.Average(x => x.Rating), Cnt = g.Count() })
            .FirstOrDefaultAsync() ?? new { Avg = 0.0, Cnt = 0 };

        var sp = await _db.SpecialistProfiles.FirstOrDefaultAsync(x => x.UserId == specialistUserId);
        if (sp is not null)
        {
            sp.AverageRating = agg.Avg;
            sp.ReviewsCount  = agg.Cnt;
            sp.UpdatedAtUtc  = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }
}
