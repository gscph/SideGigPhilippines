using Microsoft.EntityFrameworkCore;
using SideGigPh.Contracts;

namespace SideGigPh.AzureSQL;

/// <summary>
/// The SideGigPh database. Maps to the tables, keys and indexes made by <c>Sql/001-schema.sql</c> plus every later <c>NNN-schema-*.sql</c>, which are the
/// source of truth (there are no EF migrations): change both together.
/// </summary>
public sealed class SideGigPhDb(DbContextOptions<SideGigPhDb> options) : DbContext(options)
{
    public DbSet<UserRow> Users => Set<UserRow>();
    public DbSet<ProfessionalRow> Professionals => Set<ProfessionalRow>();
    public DbSet<ProfessionalServiceRow> ProfessionalServices => Set<ProfessionalServiceRow>();
    public DbSet<ProfessionalCityRow> ProfessionalCities => Set<ProfessionalCityRow>();
    public DbSet<WorkPhotoRow> ProfessionalWorkPhotos => Set<WorkPhotoRow>();
    /// <summary>Legacy (references were dropped): only Delete account still touches it, to purge old rows.</summary>
    public DbSet<ReferenceRow> ProfessionalReferences => Set<ReferenceRow>();
    public DbSet<ReviewRow> Reviews => Set<ReviewRow>();
    public DbSet<ChatRow> Chats => Set<ChatRow>();
    public DbSet<ChatMessageRow> ChatMessages => Set<ChatMessageRow>();
    public DbSet<PushDeviceRow> PushDevices => Set<PushDeviceRow>();
    public DbSet<CategoryRow> Categories => Set<CategoryRow>();
    public DbSet<ServiceRow> Services => Set<ServiceRow>();
    public DbSet<OtherJobNoteRow> OtherJobNotes => Set<OtherJobNoteRow>();
    public DbSet<ConfigOptionRow> ConfigOptions => Set<ConfigOptionRow>();
    public DbSet<BarangayRow> Barangays => Set<BarangayRow>();
    public DbSet<ErrorReportRow> ErrorReports => Set<ErrorReportRow>();
    public DbSet<ReportRow> Reports => Set<ReportRow>();
    public DbSet<BlockRow> Blocks => Set<BlockRow>();
    public DbSet<FeedbackPostRow> FeedbackPosts => Set<FeedbackPostRow>();
    public DbSet<FeedbackVoteRow> FeedbackVotes => Set<FeedbackVoteRow>();
    public DbSet<WorkerVerificationRow> WorkerVerifications => Set<WorkerVerificationRow>();
    public DbSet<VerificationAuditRow> VerificationAudit => Set<VerificationAuditRow>();
    public DbSet<JobPostRow> JobPosts => Set<JobPostRow>();
    public DbSet<JobPostPhotoRow> JobPostPhotos => Set<JobPostPhotoRow>();
    public DbSet<JobOfferRow> JobOffers => Set<JobOfferRow>();
    public DbSet<HouseholdReviewRow> HouseholdReviews => Set<HouseholdReviewRow>();

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
            // 009-schema-users-phone-index: publishing checks no other account uses the number as a Job Seeker.
            user.HasIndex(u => u.PhoneNumber).HasDatabaseName("IX_Users_PhoneNumber").IncludeProperties(u => u.Role).HasFilter("[PhoneNumber] IS NOT NULL");
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
            pro.Property(p => p.OtherWorkText).HasMaxLength(OtherWork.MaxText).IsUnicode(); // 017
            pro.Property(p => p.ProfilePhotoId).HasMaxLength(64).IsFixedLength();
            pro.HasIndex(p => p.MobileNumber).IsUnique().HasDatabaseName(Constraints.ProfessionalMobileNumber);
            pro.HasIndex(p => new { p.CategoryId, p.AreaId }).HasDatabaseName("IX_Professionals_Category_Area");
            pro.HasIndex(p => p.ProfilePhotoId).IsUnique().HasFilter("[ProfilePhotoId] IS NOT NULL")
                .HasDatabaseName(Constraints.ProfessionalProfilePhoto);

            // 013-schema-geo: the geography column Location is computed from these by the database and read only in SQL.
            pro.Property(p => p.RadiusKm).HasDefaultValue(GeoRules.DefaultRadiusKm);

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

        model.Entity<ReferenceRow>(reference =>
        {
            reference.ToTable("ProfessionalReferences");
            reference.HasKey(r => new { r.ProfessionalId, r.Position });
            reference.Property(r => r.ProfessionalId).HasMaxLength(128);
            reference.Property(r => r.Name).HasMaxLength(60).IsUnicode();
            reference.Property(r => r.MobileNumber).HasMaxLength(16);
            reference.HasOne<ProfessionalRow>().WithMany().HasForeignKey(r => r.ProfessionalId).OnDelete(DeleteBehavior.Cascade);
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

        model.Entity<PushDeviceRow>(device =>
        {
            device.ToTable("PushDevices");
            device.HasKey(d => d.Token);
            device.Property(d => d.Token).HasMaxLength(512);
            device.Property(d => d.Uid).HasMaxLength(128);
            device.Property(d => d.Platform).HasMaxLength(10);
            device.HasIndex(d => d.Uid);
        });

        model.Entity<CategoryRow>(category =>
        {
            category.ToTable("Categories");
            category.HasKey(c => c.Id);
            category.Property(c => c.Id).HasMaxLength(40);
            category.Property(c => c.LabelEnglish).HasMaxLength(60).IsUnicode();
            category.Property(c => c.LabelTaglish).HasMaxLength(60).IsUnicode();
            category.Property(c => c.Icon).HasMaxLength(16).IsUnicode();
            category.Property(c => c.HintEnglish).HasMaxLength(120).IsUnicode();
            category.Property(c => c.HintTaglish).HasMaxLength(120).IsUnicode();
        });

        model.Entity<ServiceRow>(service =>
        {
            service.ToTable("Services");
            service.HasKey(s => s.Id);
            service.Property(s => s.Id).HasMaxLength(40);
            service.Property(s => s.CategoryId).HasMaxLength(40);
            service.Property(s => s.LabelEnglish).HasMaxLength(100).IsUnicode();
            service.Property(s => s.LabelTaglish).HasMaxLength(100).IsUnicode();
            service.Property(s => s.Icon).HasMaxLength(16).IsUnicode();
            service.HasOne<CategoryRow>().WithMany().HasForeignKey(s => s.CategoryId).HasConstraintName("FK_Services_Categories");
            service.HasIndex(s => new { s.CategoryId, s.SortOrder }).HasDatabaseName("IX_Services_Category");
        });

        model.Entity<OtherJobNoteRow>(note =>
        {
            note.ToTable("OtherJobNotes");
            note.HasKey(n => n.Id);
            note.Property(n => n.Id).UseIdentityColumn();
            note.Property(n => n.Uid).HasMaxLength(128);
            note.Property(n => n.Side).HasMaxLength(12);
            note.Property(n => n.CategoryId).HasMaxLength(40);
            note.Property(n => n.Text).HasMaxLength(100).IsUnicode();
            note.HasIndex(n => new { n.CategoryId, n.CreatedAt }).HasDatabaseName("IX_OtherJobNotes_Category");
            note.HasIndex(n => n.Uid).HasDatabaseName("IX_OtherJobNotes_Uid");
        });

        model.Entity<ConfigOptionRow>(option =>
        {
            option.ToTable("ConfigOptions");
            option.HasKey(o => new { o.ListId, o.Id }).HasName("PK_ConfigOptions");
            option.Property(o => o.ListId).HasMaxLength(40);
            option.Property(o => o.Id).HasMaxLength(40);
            option.Property(o => o.LabelEnglish).HasMaxLength(100).IsUnicode();
            option.Property(o => o.LabelTaglish).HasMaxLength(100).IsUnicode();
            option.Property(o => o.Icon).HasMaxLength(16).IsUnicode();
        });

        model.Entity<BarangayRow>(barangay =>
        {
            // 021-schema-barangays
            barangay.ToTable("Barangays");
            barangay.HasKey(b => b.Id).HasName("PK_Barangays");
            barangay.Property(b => b.Id).HasMaxLength(40);
            barangay.Property(b => b.CityId).HasMaxLength(40);
            barangay.Property(b => b.LabelEnglish).HasMaxLength(60).IsUnicode();
            barangay.Property(b => b.LabelTaglish).HasMaxLength(60).IsUnicode();
            barangay.HasIndex(b => new { b.CityId, b.SortOrder }).HasDatabaseName("IX_Barangays_City");
        });

        model.Entity<ErrorReportRow>(report =>
        {
            report.ToTable("ErrorReports");
            report.HasKey(r => r.Id);
            report.Property(r => r.Id).UseIdentityColumn();
            report.Property(r => r.InstallId).HasMaxLength(36);
            report.Property(r => r.AppVersion).HasMaxLength(30);
            report.Property(r => r.CodeVersion).HasMaxLength(40);
            report.Property(r => r.Channel).HasMaxLength(10);
            report.Property(r => r.Platform).HasMaxLength(30);
            report.Property(r => r.Device).HasMaxLength(80).IsUnicode();
            report.Property(r => r.Kind).HasMaxLength(12);
            report.Property(r => r.Source).HasMaxLength(200);
            report.Property(r => r.Message).HasMaxLength(1000).IsUnicode();
            report.Property(r => r.Detail).HasMaxLength(4000).IsUnicode();
            report.Property(r => r.Route).HasMaxLength(200);
            report.Property(r => r.Trail).HasMaxLength(2000).IsUnicode();
            report.HasIndex(r => r.ReceivedAt).HasDatabaseName("IX_ErrorReports_ReceivedAt");
            report.HasIndex(r => new { r.Source, r.Channel, r.CodeVersion }).HasDatabaseName("IX_ErrorReports_Source");
        });

        model.Entity<ReportRow>(report =>
        {
            report.ToTable("Reports");
            report.HasKey(r => r.Id).HasName("PK_Reports");
            report.Property(r => r.Id).UseIdentityColumn();
            report.Property(r => r.ReporterUid).HasMaxLength(128);
            report.Property(r => r.TargetUid).HasMaxLength(128);
            report.Property(r => r.Kind).HasMaxLength(10);
            report.Property(r => r.SubjectId).HasMaxLength(260);
            report.Property(r => r.Reason).HasMaxLength(20);
            report.Property(r => r.Note).HasMaxLength(500).IsUnicode();
            report.Property(r => r.Status).HasMaxLength(10);
            report.HasIndex(r => new { r.Status, r.CreatedAt }).HasDatabaseName("IX_Reports_Status");
        });

        model.Entity<BlockRow>(block =>
        {
            block.ToTable("Blocks");
            block.HasKey(b => new { b.BlockerUid, b.BlockedUid }).HasName("PK_Blocks");
            block.Property(b => b.BlockerUid).HasMaxLength(128);
            block.Property(b => b.BlockedUid).HasMaxLength(128);
            block.HasIndex(b => b.BlockedUid).HasDatabaseName("IX_Blocks_Blocked");
        });

        model.Entity<FeedbackPostRow>(post =>
        {
            post.ToTable("FeedbackPosts");
            post.HasKey(p => p.Id).HasName("PK_FeedbackPosts");
            post.Property(p => p.Id).UseIdentityColumn();
            post.Property(p => p.AuthorUid).HasMaxLength(128);
            post.Property(p => p.Side).HasMaxLength(20);
            post.Property(p => p.Title).HasMaxLength(100).IsUnicode();
            post.Property(p => p.Details).HasMaxLength(600).IsUnicode();
            post.Property(p => p.Status).HasMaxLength(10);
            post.HasIndex(p => new { p.Status, p.Votes, p.CreatedAt }).IsDescending(false, true, true).HasDatabaseName("IX_FeedbackPosts_Top");
            post.HasIndex(p => p.AuthorUid).HasFilter("[AuthorUid] IS NOT NULL").HasDatabaseName("IX_FeedbackPosts_Author");
        });

        model.Entity<FeedbackVoteRow>(vote =>
        {
            vote.ToTable("FeedbackVotes");
            vote.HasKey(v => new { v.PostId, v.VoterUid }).HasName("PK_FeedbackVotes");
            vote.Property(v => v.VoterUid).HasMaxLength(128);
            vote.HasOne<FeedbackPostRow>().WithMany().HasForeignKey(v => v.PostId).HasConstraintName("FK_FeedbackVotes_Post").OnDelete(DeleteBehavior.Cascade);
            vote.HasIndex(v => v.VoterUid).HasDatabaseName("IX_FeedbackVotes_Voter");
        });

        model.Entity<WorkerVerificationRow>(verification =>
        {
            verification.ToTable("WorkerVerifications");
            verification.HasKey(v => v.Id).HasName("PK_WorkerVerifications");
            verification.Property(v => v.Id).UseIdentityColumn();
            verification.Property(v => v.Uid).HasMaxLength(128);
            verification.Property(v => v.Kind).HasMaxLength(12);
            verification.Property(v => v.BadgeId).HasMaxLength(40);
            verification.Property(v => v.CompanyName).HasMaxLength(60).IsUnicode();
            verification.Property(v => v.ProjectName).HasMaxLength(100).IsUnicode();
            verification.Property(v => v.Status).HasMaxLength(10);
            verification.Property(v => v.DocumentBlob).HasMaxLength(200);
            verification.Property(v => v.ScanStatus).HasMaxLength(12);
            verification.Property(v => v.ScannedType).HasMaxLength(60);
            verification.Property(v => v.ScannedName).HasMaxLength(120).IsUnicode();
            verification.Property(v => v.ScannedNumber).HasMaxLength(60).IsUnicode();
            verification.Property(v => v.DecidedBy).HasMaxLength(128);
            verification.Property(v => v.RejectReason).HasMaxLength(200).IsUnicode();
            verification.HasIndex(v => new { v.Uid, v.Kind, v.BadgeId, v.CompanyName }).IsUnique().HasFilter("[Status] = 'pending'")
                .HasDatabaseName(Constraints.VerificationPending);
            verification.HasIndex(v => new { v.Uid, v.Status }).HasDatabaseName("IX_WorkerVerifications_Uid");
            verification.HasIndex(v => new { v.Status, v.CreatedAt }).HasDatabaseName("IX_WorkerVerifications_Status");
        });

        model.Entity<VerificationAuditRow>(audit =>
        {
            audit.ToTable("VerificationAudit");
            audit.HasKey(a => a.Id).HasName("PK_VerificationAudit");
            audit.Property(a => a.Id).UseIdentityColumn();
            audit.Property(a => a.ActorUid).HasMaxLength(128);
            audit.Property(a => a.Action).HasMaxLength(12);
            audit.HasIndex(a => new { a.VerificationId, a.At }).HasDatabaseName("IX_VerificationAudit_Verification");
        });

        model.Entity<JobPostRow>(post =>
        {
            // 015-schema-job-posts: the geography column Location is computed from Latitude / Longitude by the
            // database and read only in SQL (the workers' feed).
            post.ToTable("JobPosts");
            post.HasKey(p => p.Id).HasName("PK_JobPosts");
            post.Property(p => p.Id).UseIdentityColumn();
            post.Property(p => p.SeekerUid).HasMaxLength(128);
            post.Property(p => p.CategoryId).HasMaxLength(40);
            post.Property(p => p.ServiceId).HasMaxLength(40);
            post.Property(p => p.Description).HasMaxLength(600).IsUnicode();
            post.Property(p => p.CityId).HasMaxLength(40);
            post.Property(p => p.BarangayId).HasMaxLength(40);
            post.Property(p => p.NeededOn).HasColumnType("date");
            post.Property(p => p.Status).HasMaxLength(10);
            post.HasMany(p => p.Photos).WithOne().HasForeignKey(photo => photo.PostId).HasConstraintName("FK_JobPostPhotos_Post").OnDelete(DeleteBehavior.Cascade);
            post.HasIndex(p => new { p.Status, p.CategoryId, p.CreatedAt }).IsDescending(false, false, true).HasDatabaseName("IX_JobPosts_Open");
            post.HasIndex(p => new { p.SeekerUid, p.CreatedAt }).IsDescending(false, true).HasDatabaseName("IX_JobPosts_Seeker");
        });

        model.Entity<JobPostPhotoRow>(photo =>
        {
            photo.ToTable("JobPostPhotos");
            photo.HasKey(p => new { p.PostId, p.PhotoId }).HasName("PK_JobPostPhotos");
            photo.Property(p => p.PhotoId).HasMaxLength(64);
        });

        model.Entity<JobOfferRow>(offer =>
        {
            // 016-schema-job-offers
            offer.ToTable("JobOffers");
            offer.HasKey(o => o.Id).HasName("PK_JobOffers");
            offer.Property(o => o.Id).UseIdentityColumn();
            offer.Property(o => o.ProUid).HasMaxLength(128);
            offer.Property(o => o.Note).HasMaxLength(300).IsUnicode();
            offer.Property(o => o.Status).HasMaxLength(10);
            offer.HasOne<JobPostRow>().WithMany().HasForeignKey(o => o.PostId).HasConstraintName("FK_JobOffers_Post").OnDelete(DeleteBehavior.Cascade);
            offer.HasIndex(o => new { o.PostId, o.ProUid }).IsUnique().HasDatabaseName("UX_JobOffers_PostPro");
            offer.HasIndex(o => o.ProUid).HasDatabaseName("IX_JobOffers_Pro");
            // 018-schema-job-done
            offer.HasIndex(o => o.ProUid, "IX_JobOffers_Done").HasFilter("[HouseholdDoneAt] IS NOT NULL AND [WorkerDoneAt] IS NOT NULL");
        });

        model.Entity<HouseholdReviewRow>(review =>
        {
            // 018-schema-job-done
            review.ToTable("HouseholdReviews");
            review.HasKey(r => new { r.ProUid, r.SeekerUid }).HasName("PK_HouseholdReviews");
            review.Property(r => r.ProUid).HasMaxLength(128);
            review.Property(r => r.SeekerUid).HasMaxLength(128);
            review.Property(r => r.Comment).HasMaxLength(1000).IsUnicode();
            review.HasIndex(r => r.SeekerUid).HasDatabaseName("IX_HouseholdReviews_Seeker").IncludeProperties(r => r.Rating);
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
    public const string PushDeviceKey = "PK_PushDevices";
    public const string VerificationPending = "UX_WorkerVerifications_Pending";
}
