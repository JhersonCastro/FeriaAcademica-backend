using FeriaAcademica.Domain.Events;
using FeriaAcademica.Domain.Notifications;
using FeriaAcademica.Domain.Privacy;
using FeriaAcademica.Domain.Registrations;
using FeriaAcademica.Domain.Works;
using FeriaAcademica.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FeriaAcademica.Infrastructure.Persistence.Configurations;

internal sealed class RegistrationConfiguration : IEntityTypeConfiguration<Registration>
{
    public void Configure(EntityTypeBuilder<Registration> b)
    {
        b.ToTable("Registrations");
        b.Property(x => x.QrToken).HasMaxLength(200).IsRequired();
        b.HasOne<AcademicEvent>().WithMany().HasForeignKey(x => x.EventId);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UserId);
        b.HasIndex(x => x.QrToken).IsUnique();
        b.HasIndex(x => new { x.EventId, x.UserId }).IsUnique();
    }
}

internal sealed class AttendanceConfiguration : IEntityTypeConfiguration<Attendance>
{
    public void Configure(EntityTypeBuilder<Attendance> b)
    {
        b.ToTable("Attendances");
        b.HasOne<Registration>().WithMany().HasForeignKey(x => x.RegistrationId);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ValidatedBy);
        b.HasIndex(x => x.RegistrationId).IsUnique();
    }
}

internal sealed class CertificateConfiguration : IEntityTypeConfiguration<Certificate>
{
    public void Configure(EntityTypeBuilder<Certificate> b)
    {
        b.ToTable("Certificates", t => t.HasCheckConstraint("CK_Certificates_WorkOnlyForParticipation", "(\"Type\" = 'Participation') = (\"WorkId\" IS NOT NULL)"));
        b.Property(x => x.Code).HasMaxLength(100).IsRequired();
        b.HasOne<AcademicEvent>().WithMany().HasForeignKey(x => x.EventId);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UserId);
        b.HasOne<Work>().WithMany().HasForeignKey(x => x.WorkId);
        b.HasIndex(x => x.Code).IsUnique();
    }
}

internal sealed class DataPolicyConfiguration : IEntityTypeConfiguration<DataPolicy>
{
    public void Configure(EntityTypeBuilder<DataPolicy> b)
    {
        b.ToTable("DataPolicies");
        b.Property(x => x.Version).HasMaxLength(50).IsRequired();
        b.HasIndex(x => x.Version).IsUnique();
    }
}

internal sealed class ConsentConfiguration : IEntityTypeConfiguration<Consent>
{
    public void Configure(EntityTypeBuilder<Consent> b)
    {
        b.ToTable("Consents");
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UserId);
        b.HasOne<DataPolicy>().WithMany().HasForeignKey(x => x.PolicyId);
        b.HasIndex(x => new { x.UserId, x.PolicyId }).IsUnique();
    }
}

internal sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> b)
    {
        b.ToTable("Notifications");
        b.Property(x => x.Type).HasMaxLength(100).IsRequired();
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UserId);
        b.HasOne<AcademicEvent>().WithMany().HasForeignKey(x => x.EventId);
    }
}
