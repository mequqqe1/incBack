using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace SharpAuthDemo.Data;

public static class SeedData
{
    public static async Task EnsureSeededAsync(IServiceProvider sp)
    {
        using var scope = sp.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        string[] roles = ["Specialist", "Parent", "Admin"];
        foreach (var r in roles)
            if (!await roleManager.RoleExistsAsync(r))
                await roleManager.CreateAsync(new IdentityRole(r));

        if (!await db.Specializations.AnyAsync())
        {
            db.Specializations.AddRange(
                new() { Name = "Логопед", SortOrder = 10 },
                new() { Name = "Дефектолог", SortOrder = 20 },
                new() { Name = "Эрготерапевт", SortOrder = 30 },
                new() { Name = "Клинический психолог", SortOrder = 40 }
            );
        }
        if (!await db.Skills.AnyAsync())
        {
            db.Skills.AddRange(
                new() { Name = "ABA-терапия", SortOrder = 10 },
                new() { Name = "PECS", SortOrder = 20 },
                new() { Name = "Сенсорная интеграция", SortOrder = 30 },
                new() { Name = "Ранняя помощь", SortOrder = 40 }
            );
        }

        await db.SaveChangesAsync();
    }
}