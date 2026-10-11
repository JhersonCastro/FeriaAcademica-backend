using FeriaAcademica.Domain.Institutions;
using FeriaAcademica.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FeriaAcademica.Infrastructure.Persistence.Configurations;

internal sealed class InstitutionConfiguration : IEntityTypeConfiguration<Institution>
{
    public void Configure(EntityTypeBuilder<Institution> b)
    {
        b.ToTable("Institutions");
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.Property(x => x.Acronym).HasMaxLength(30);
        b.HasIndex(x => x.Name).IsUnique();
    }
}

internal sealed class InstitutionMembershipConfiguration : IEntityTypeConfiguration<InstitutionMembership>
{
    public void Configure(EntityTypeBuilder<InstitutionMembership> b)
    {
        b.ToTable("InstitutionMemberships");
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UserId);
        b.HasOne<Institution>().WithMany().HasForeignKey(x => x.InstitutionId);
        b.HasIndex(x => new { x.UserId, x.InstitutionId }).IsUnique();
    }
}
