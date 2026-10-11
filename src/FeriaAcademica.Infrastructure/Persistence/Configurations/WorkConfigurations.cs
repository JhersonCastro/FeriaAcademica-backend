using FeriaAcademica.Domain.Events;
using FeriaAcademica.Domain.Works;
using FeriaAcademica.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FeriaAcademica.Infrastructure.Persistence.Configurations;

internal sealed class WorkConfiguration : IEntityTypeConfiguration<Work>
{
    public void Configure(EntityTypeBuilder<Work> b)
    {
        b.ToTable("Works", t =>
        {
            t.HasCheckConstraint("CK_Works_MaxRejections", "\"MaxRejections\" >= 1");
            t.HasCheckConstraint("CK_Works_RejectedDefinitively",
                "\"Status\" <> 'RejectedDefinitively' OR (\"Closure\" IN ('Manual', 'ByAttempts') AND \"ClosureReason\" IS NOT NULL)");
            t.HasCheckConstraint("CK_Works_Cancelled", "\"Status\" <> 'Cancelled' OR \"Closure\" = 'Withdrawal'");
        });
        b.Property(x => x.Title).HasMaxLength(300).IsRequired();
        b.Property(x => x.Prize).HasMaxLength(200);
        b.HasOne<Participation>().WithMany().HasForeignKey(x => x.ParticipationId);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ProfessorId);
        b.HasMany(x => x.Artifacts).WithOne().HasForeignKey(a => a.WorkId);
        b.HasMany(x => x.History).WithOne().HasForeignKey(h => h.WorkId);
        b.HasMany(x => x.ReviewQueue).WithOne().HasForeignKey(q => q.WorkId);
    }
}

internal sealed class ArtifactConfiguration : IEntityTypeConfiguration<Artifact>
{
    public void Configure(EntityTypeBuilder<Artifact> b)
    {
        b.ToTable("Artifacts", t => t.HasCheckConstraint("CK_Artifacts_FileOrLink", "(\"FileUrl\" IS NULL) <> (\"LinkUrl\" IS NULL)"));
        b.HasIndex(x => new { x.WorkId, x.Type, x.Version }).IsUnique();
    }
}

internal sealed class WorkHistoryEntryConfiguration : IEntityTypeConfiguration<WorkHistoryEntry>
{
    public void Configure(EntityTypeBuilder<WorkHistoryEntry> b)
    {
        b.ToTable("WorkHistory");
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ActorId);
    }
}

internal sealed class ReviewQueueItemConfiguration : IEntityTypeConfiguration<ReviewQueueItem>
{
    public void Configure(EntityTypeBuilder<ReviewQueueItem> b)
    {
        b.ToTable("ReviewQueue");
        b.HasIndex(x => new { x.WorkId, x.Attempt }).IsUnique();
    }
}

internal sealed class JuryAssignmentConfiguration : IEntityTypeConfiguration<JuryAssignment>
{
    public void Configure(EntityTypeBuilder<JuryAssignment> b)
    {
        b.ToTable("JuryAssignments");
        b.HasOne<Work>().WithMany().HasForeignKey(x => x.WorkId);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.JuryId);
        b.HasIndex(x => new { x.WorkId, x.JuryId }).IsUnique();
    }
}
