// Data/AppDbContext.cs
using INCBack.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SharpAuthDemo.Models;

namespace SharpAuthDemo.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole, string>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<SpecialistProfile> SpecialistProfiles => Set<SpecialistProfile>();
    public DbSet<SpecialistDiploma> SpecialistDiplomas => Set<SpecialistDiploma>();
    public DbSet<Specialization> Specializations => Set<Specialization>();
    public DbSet<Skill> Skills => Set<Skill>();
    public DbSet<SpecialistSpecialization> SpecialistSpecializations => Set<SpecialistSpecialization>();
    public DbSet<SpecialistSkill> SpecialistSkills => Set<SpecialistSkill>();
    public DbSet<AvailabilitySlot> AvailabilitySlots => Set<AvailabilitySlot>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<WeeklyTemplate> WeeklyTemplates => Set<WeeklyTemplate>();
    public DbSet<WeeklyTemplateSlot> WeeklyTemplateSlots => Set<WeeklyTemplateSlot>();
    public DbSet<BookingOutcome> BookingOutcomes => Set<BookingOutcome>();
    // Новые
    public DbSet<ParentProfile> ParentProfiles => Set<ParentProfile>();
    public DbSet<Child> Children => Set<Child>();
    public DbSet<ChildNote> ChildNotes => Set<ChildNote>();
    public DbSet<CaregiverMember> CaregiverMembers => Set<CaregiverMember>();
    public DbSet<ChildDocument> ChildDocuments => Set<ChildDocument>();
    public DbSet<SpecialistReview> SpecialistReviews => Set<SpecialistReview>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // короткие имена для Identity
        builder.Entity<ApplicationUser>().ToTable("users");
        builder.Entity<IdentityRole>().ToTable("roles");
        builder.Entity<IdentityUserRole<string>>().ToTable("user_roles");
        builder.Entity<IdentityUserLogin<string>>().ToTable("user_logins");
        builder.Entity<IdentityUserToken<string>>().ToTable("user_tokens");
        builder.Entity<IdentityUserClaim<string>>().ToTable("user_claims");
        builder.Entity<IdentityRoleClaim<string>>().ToTable("role_claims");

        builder.Entity<AvailabilitySlot>(e =>
        {
            e.ToTable("availability_slots");
            e.HasIndex(x => new { x.SpecialistUserId, x.StartsAtUtc }).IsUnique();
            e.HasCheckConstraint("CK_Availability_Time", "\"EndsAtUtc\" > \"StartsAtUtc\"");
        });

        // ====== СПЕЦЫ (как у тебя) ======
        builder.Entity<SpecialistProfile>(e =>
        {
            e.ToTable("specialist_profiles");
            e.HasIndex(p => p.UserId).IsUnique();
            e.Property(p => p.Status).HasConversion<int>();
            e.Property(p => p.CountryCode).HasMaxLength(2).IsRequired();
            e.Property(p => p.City).HasMaxLength(200).IsRequired();
            e.Property(p => p.AddressLine1).HasMaxLength(300).IsRequired();

            e.HasMany(p => p.Diplomas)
             .WithOne(d => d.SpecialistProfile)
             .HasForeignKey(d => d.SpecialistProfileId)
             .OnDelete(DeleteBehavior.SetNull);

            e.HasMany(p => p.SpecialistSpecializations)
             .WithOne(ss => ss.SpecialistProfile)
             .HasForeignKey(ss => ss.SpecialistProfileId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasMany(p => p.SpecialistSkills)
             .WithOne(sk => sk.SpecialistProfile)
             .HasForeignKey(sk => sk.SpecialistProfileId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Specialization>(e =>
        {
            e.ToTable("specializations");
            e.HasIndex(x => x.Name).IsUnique();
        });

        builder.Entity<Skill>(e =>
        {
            e.ToTable("skills");
            e.HasIndex(x => x.Name).IsUnique();
        });

        builder.Entity<SpecialistSpecialization>(e =>
        {
            e.ToTable("specialist_profile_specializations");
            e.HasIndex(x => new { x.SpecialistProfileId, x.SpecializationId }).IsUnique();
        });

        builder.Entity<SpecialistSkill>(e =>
        {
            e.ToTable("specialist_profile_skills");
            e.HasIndex(x => new { x.SpecialistProfileId, x.SkillId }).IsUnique();
        });

        builder.Entity<Booking>(e =>
        {
            e.ToTable("bookings");
            e.Property(b => b.Status).HasConversion<int>();
            e.HasIndex(b => new { b.SpecialistUserId, b.StartsAtUtc });
            e.HasIndex(b => new { b.ParentUserId, b.StartsAtUtc });
            e.HasOne(b => b.AvailabilitySlot).WithMany()
                .HasForeignKey(b => b.AvailabilitySlotId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasCheckConstraint("CK_Booking_Time", "\"EndsAtUtc\" > \"StartsAtUtc\"");
        });

        builder.Entity<WeeklyTemplate>(e =>
        {
            e.ToTable("weekly_templates");
            e.HasIndex(x => x.SpecialistUserId).IsUnique();
        });

        builder.Entity<WeeklyTemplateSlot>(e =>
        {
            e.ToTable("weekly_template_slots");
            e.HasIndex(x => new { x.WeeklyTemplateId, x.DayOfWeek, x.StartLocalTime, x.EndLocalTime }).IsUnique();
            e.HasCheckConstraint("CK_WeeklyTemplateSlot_Time", "\"EndLocalTime\" > \"StartLocalTime\"");
        });

        // ====== РОДИТЕЛИ/ДЕТИ/ЧЛЕНЫ/ДОКИ ======
        builder.Entity<ParentProfile>(e =>
        {
            e.ToTable("parent_profiles");
            e.HasIndex(x => x.UserId).IsUnique();

            e.HasOne(p => p.User)
             .WithOne()
             .HasForeignKey<ParentProfile>(p => p.UserId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasMany(p => p.Members)
             .WithOne(m => m.ParentProfile)
             .HasForeignKey(m => m.ParentProfileId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Child>(e =>
        {
            e.ToTable("children");
            e.HasIndex(x => new { x.ParentProfileId, x.FirstName, x.BirthDate });

            e.Property(x => x.Sex).HasConversion<int>();
            e.Property(x => x.SupportLevel).HasConversion<int>();
            e.HasCheckConstraint("CK_Child_BirthDate",
                "\"BirthDate\" IS NULL OR \"BirthDate\" < CURRENT_TIMESTAMP");

            e.HasOne(c => c.ParentProfile)
             .WithMany(p => p.Children)
             .HasForeignKey(c => c.ParentProfileId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ChildNote>(e =>
        {
            e.ToTable("child_notes");
            e.HasIndex(x => x.ChildId);
            e.HasOne(n => n.Child)
             .WithMany(c => c.Notes)
             .HasForeignKey(n => n.ChildId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<CaregiverMember>(e =>
        {
            e.ToTable("caregiver_members");
            e.Property(x => x.Status).HasConversion<int>();
            e.Property(x => x.Email).HasMaxLength(320).IsRequired();
            e.Property(x => x.Relation).HasMaxLength(100);
            e.HasIndex(x => new { x.ParentProfileId, x.Email }).IsUnique();
            e.HasIndex(x => new { x.ParentProfileId, x.UserId });
            e.HasOne(m => m.User)
             .WithMany()
             .HasForeignKey(m => m.UserId)
             .OnDelete(DeleteBehavior.SetNull);
        });
        builder.Entity<Booking>()
            .HasOne(x => x.Outcome)
            .WithOne(x => x.Booking)
            .HasForeignKey<BookingOutcome>(x => x.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<BookingOutcome>()
            .HasIndex(x => x.BookingId)
            .IsUnique();

        builder.Entity<ChildDocument>(e =>
        {
            e.ToTable("child_documents");
            e.Property(x => x.ContentType).HasMaxLength(100).IsRequired();
            e.Property(x => x.FileName).HasMaxLength(255).IsRequired();
            e.Property(x => x.ContentBase64).HasColumnType("text");
            e.HasIndex(x => x.ChildId);
            e.HasOne(d => d.Child)
             .WithMany(c => c.Documents)
             .HasForeignKey(d => d.ChildId)
             .OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<SpecialistReview>(e =>
        {
            e.ToTable("specialist_reviews");
            e.HasIndex(x => new { x.SpecialistUserId, x.CreatedAtUtc });
            e.HasIndex(x => x.BookingId).IsUnique(false);
            e.HasCheckConstraint("CK_Review_Rating", "\"Rating\" BETWEEN 1 AND 5");

            e.HasOne(x => x.Booking)
                .WithOne(bk => bk.Review)            // ← теперь свойство существует
                .HasForeignKey<SpecialistReview>(x => x.BookingId)
                .OnDelete(DeleteBehavior.SetNull);
        });
        
    }
}
