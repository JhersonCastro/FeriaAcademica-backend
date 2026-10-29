using FeriaAcademica.Domain.Events;
using FeriaAcademica.Domain.Institutions;
using FeriaAcademica.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FeriaAcademica.Infrastructure.Persistence.Configurations;

internal sealed class EventTemplateConfiguration : IEntityTypeConfiguration<EventTemplate>
{
    public void Configure(EntityTypeBuilder<EventTemplate> b)
    {
        b.ToTable("EventTemplates");
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.Property(x => x.ConfigJson).HasColumnType("jsonb").IsRequired();
        b.HasOne<Institution>().WithMany().HasForeignKey(x => x.InstitutionId);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.CreatedBy);
    }
}

internal sealed class AcademicEventConfiguration : IEntityTypeConfiguration<AcademicEvent>
{
    public void Configure(EntityTypeBuilder<AcademicEvent> b)
    {
        b.ToTable("Events", t =>
        {
            t.HasCheckConstraint("CK_Events_MaxRejections", "\"MaxRejections\" >= 1");
            t.HasCheckConstraint("CK_Events_Capacity", "\"Capacity\" IS NULL OR \"Capacity\" >= 1");
            t.HasCheckConstraint("CK_Events_Weights", "ABS(\"JuryWeight\" + \"AttendeeWeight\" - 1.0) < 1e-9");
            t.HasCheckConstraint("CK_Events_CancellationReason", "\"Status\" <> 'Cancelled' OR \"CancellationReason\" IS NOT NULL");
        });
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.Property(x => x.TieBreakRulesJson).HasColumnType("jsonb").IsRequired();
        b.HasOne<Institution>().WithMany().HasForeignKey(x => x.InstitutionId);
        b.HasOne<EventTemplate>().WithMany().HasForeignKey(x => x.TemplateId);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.CreatedBy);
        b.HasMany(x => x.Stages).WithOne().HasForeignKey(s => s.EventId);
    }
}

internal sealed class EventStageConfiguration : IEntityTypeConfiguration<EventStage>
{
    public void Configure(EntityTypeBuilder<EventStage> b)
    {
        b.ToTable("EventStages");
        b.HasIndex(x => new { x.EventId, x.Type }).IsUnique();
    }
}

internal sealed class RoleAssignmentConfiguration : IEntityTypeConfiguration<RoleAssignment>
{
    public void Configure(EntityTypeBuilder<RoleAssignment> b)
    {
        b.ToTable("RoleAssignments");
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UserId);
        b.HasOne<AcademicEvent>().WithMany().HasForeignKey(x => x.EventId);
        b.HasIndex(x => new { x.UserId, x.EventId, x.Role }).IsUnique();
    }
}

internal sealed class InvitationConfiguration : IEntityTypeConfiguration<Invitation>
{
    public void Configure(EntityTypeBuilder<Invitation> b)
    {
        b.ToTable("Invitations");
        b.Property(x => x.Email).HasMaxLength(320).IsRequired();
        b.Property(x => x.Token).HasMaxLength(200).IsRequired();
        b.HasOne<AcademicEvent>().WithMany().HasForeignKey(x => x.EventId);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.SenderId);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UserId);
        b.HasIndex(x => x.Token).IsUnique();
    }
}

internal sealed class ParticipationConfiguration : IEntityTypeConfiguration<Participation>
{
    public void Configure(EntityTypeBuilder<Participation> b)
    {
        b.ToTable("Participations");
        b.HasOne<AcademicEvent>().WithMany().HasForeignKey(x => x.EventId);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UserId);
        b.HasOne<Invitation>().WithMany().HasForeignKey(x => x.InvitationId);
        b.HasIndex(x => new { x.EventId, x.UserId }).IsUnique();
    }
}
