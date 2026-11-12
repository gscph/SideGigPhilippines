using Microsoft.EntityFrameworkCore;

namespace SideGigPh.AzureSQL;

/// <summary>
/// The SideGigPh database. Maps to the tables, keys and indexes made by <c>Sql/001-schema.sql</c>, which is the
/// source of truth (there are no EF migrations): change both together.
/// </summary>
public sealed class SideGigPhDb(DbContextOptions<SideGigPhDb> options) : DbContext(options)
{
    public DbSet<UserRow> Users => Set<UserRow>();
    public DbSet<ProfessionalRow> Professionals => Set<ProfessionalRow>();
    public DbSet<ProfessionalServiceRow> ProfessionalServices => Set<ProfessionalServiceRow>();
    public DbSet<ProfessionalCityRow> ProfessionalCities => Set<ProfessionalCityRow>();
    public DbSet<WorkPhotoRow> ProfessionalWorkPhotos => Set<WorkPhotoRow>();
    public DbSet<ReviewRow> Reviews => Set<ReviewRow>();
    public DbSet<ChatRow> Chats => Set<ChatRow>();
    public DbSet<ChatMessageRow> ChatMessages => Set<ChatMessageRow>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configuration)
    {
        // Ids and codes are ASCII (varchar); names and free text are nvarchar, set per column below.
        configuration.Properties<string>().AreUnicode(false);
        configuration.Properties<DateTimeOffset>().HavePrecision(3);
    }

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<UserRow>(user =>
        {
            user.ToTable("Users");
            user.HasKey(u => u.Uid);
            user.Property(u => u.Uid).HasMaxLength(128);
            user.Property(u => u.DisplayName).HasMaxLength(80).IsUnicode();
            user.Property(u => u.PhoneNumber).HasMaxLength(16);
            user.Property(u => u.Email).HasMaxLength(254).IsUnicode();
            user.Property(u => u.Language).HasMaxLength(10);
            user.Property(u => u.Role).HasMaxLength(12);
            user.Property(u => u.IncomeGoalId).HasMaxLength(40);
            user.Property(u => u.ProfilePhotoId).HasMaxLength(64).IsFixedLength();
            user.HasIndex(u => u.ProfilePhotoId).IsUnique().HasDatabaseName(Constraints.UserProfilePhoto).HasFilter("[ProfilePhotoId] IS NOT NULL");
        });

        model.Entity<ProfessionalRow>(pro =>
        {
            pro.ToTable("Professionals");
            pro.HasKey(p => p.Id);
            pro.Property(p => p.Id).HasMaxLength(128);
            pro.Property(p => p.DisplayName).HasMaxLength(60).IsUnicode();
            pro.Property(p => p.MobileNumber).HasMaxLength(16);
            pro.Property(p => p.CategoryId).HasMaxLength(40);
            pro.Property(p => p.AreaId).HasMaxLength(40);
            pro.Property(p => p.ExperienceId).HasMaxLength(40);
            pro.Property(p => p.RateTierId).HasMaxLength(40);
            pro.Property(p => p.ProfilePhotoId).HasMaxLength(64).IsFixedLength();
            pro.HasIndex(p => p.MobileNumber).IsUnique().HasDatabaseName(Constraints.ProfessionalMobileNumber);
            pro.HasIndex(p => new { p.CategoryId, p.AreaId }).HasDatabaseName("IX_Professionals_Category_Area");
            pro.HasIndex(p => p.ProfilePhotoId).IsUnique().HasFilter("[ProfilePhotoId] IS NOT NULL")
                .HasDatabaseName(Constraints.ProfessionalProfilePhoto);

            pro.HasMany(p => p.Services).WithOne().HasForeignKey(s => s.ProfessionalId).OnDelete(DeleteBehavior.Cascade);
            pro.HasMany(p => p.Cities).WithOne().HasForeignKey(c => c.ProfessionalId).OnDelete(DeleteBehavior.Cascade);
            pro.HasMany(p => p.WorkPhotos).WithOne().HasForeignKey(w => w.ProfessionalId).OnDelete(DeleteBehavior.Cascade);
        });

        model.Entity<ProfessionalServiceRow>(service =>
        {
            service.ToTable("ProfessionalServices");
            service.HasKey(s => new { s.ProfessionalId, s.ServiceId });
            service.Property(s => s.ProfessionalId).HasMaxLength(128);
            service.Property(s => s.ServiceId).HasMaxLength(40);
        });

        model.Entity<ProfessionalCityRow>(city =>
        {
            city.ToTable("ProfessionalCities");
            city.HasKey(c => new { c.ProfessionalId, c.CityId });
            city.Property(c => c.ProfessionalId).HasMaxLength(128);
            city.Property(c => c.CityId).HasMaxLength(40);
        });

        model.Entity<WorkPhotoRow>(photo =>
        {
            photo.ToTable("ProfessionalWorkPhotos");
            photo.HasKey(w => w.PhotoId).HasName(Constraints.WorkPhotoKey).IsClustered(false);
            photo.Property(w => w.PhotoId).HasMaxLength(64).IsFixedLength();
            photo.Property(w => w.ProfessionalId).HasMaxLength(128);
            photo.HasIndex(w => new { w.ProfessionalId, w.Position }).IsUnique().IsClustered();
        });

        model.Entity<ReviewRow>(review =>
        {
            review.ToTable("Reviews");
            review.HasKey(r => new { r.ProfessionalId, r.SeekerId });
            review.Property(r => r.ProfessionalId).HasMaxLength(128);
            review.Property(r => r.SeekerId).HasMaxLength(128);
            review.Property(r => r.ReviewerName).HasMaxLength(60).IsUnicode();
            review.Property(r => r.Comment).HasMaxLength(1000).IsUnicode();
            review.Property(r => r.ServiceId).HasMaxLength(40);
            review.HasIndex(r => new { r.SeekerId, r.CreatedAt }).IsDescending(false, true);
        });

        model.Entity<ChatRow>(chat =>
        {
            chat.ToTable("Chats");
            chat.HasKey(c => c.Id);
            chat.Property(c => c.Id).HasMaxLength(257);
            chat.Property(c => c.ProfessionalId).HasMaxLength(128);
            chat.Property(c => c.SeekerId).HasMaxLength(128);
            chat.Property(c => c.ProfessionalName).HasMaxLength(60).IsUnicode();
            chat.Property(c => c.SeekerName).HasMaxLength(60).IsUnicode();
            chat.Property(c => c.ServiceId).HasMaxLength(40);
            chat.Property(c => c.CityId).HasMaxLength(40);
            chat.Property(c => c.UrgencyId).HasMaxLength(40);
            chat.Property(c => c.BudgetId).HasMaxLength(40);
            chat.Property(c => c.LastMessageId).HasMaxLength(64);
            chat.Property(c => c.LastMessageSenderId).HasMaxLength(128);
            chat.Property(c => c.LastMessageKind).HasMaxLength(4);
            chat.Property(c => c.LastMessageText).HasMaxLength(1000).IsUnicode();
            chat.HasIndex(c => new { c.ProfessionalId, c.SeekerId }).IsUnique();
            chat.HasIndex(c => new { c.SeekerId, c.LastMessageAt }).IsDescending(false, true);
            chat.HasIndex(c => new { c.ProfessionalId, c.LastMessageAt }).IsDescending(false, true);
        });

        model.Entity<ChatMessageRow>(message =>
        {
            message.ToTable("ChatMessages");
            message.HasKey(m => m.Id).IsClustered(false);
            message.Property(m => m.Id).HasMaxLength(64);
            message.Property(m => m.ChatId).HasMaxLength(257);
            message.Property(m => m.SenderId).HasMaxLength(128);
            message.Property(m => m.Kind).HasMaxLength(4);
            message.Property(m => m.Text).HasMaxLength(1000).IsUnicode();
            message.HasIndex(m => new { m.ChatId, m.At }).IsClustered();
            message.HasOne<ChatRow>().WithMany().HasForeignKey(m => m.ChatId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}

/// <summary>Constraint names from <c>001-schema.sql</c> that the stores turn into answers (anti-fraud rules 1 and 2).</summary>
internal static class Constraints
{
    public const string ProfessionalMobileNumber = "UQ_Professionals_MobileNumber";
    public const string ProfessionalProfilePhoto = "UX_Professionals_ProfilePhoto";
    public const string UserProfilePhoto = "UX_Users_ProfilePhoto";
    public const string WorkPhotoKey = "PK_ProfessionalWorkPhotos";
}
