using FeriaAcademica.Domain.Evaluation;
using FeriaAcademica.Domain.Events;
using FeriaAcademica.Domain.Works;
using FeriaAcademica.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FeriaAcademica.Infrastructure.Persistence.Configurations;

internal sealed class InstrumentConfiguration : IEntityTypeConfiguration<Instrument>
{
    public void Configure(EntityTypeBuilder<Instrument> b)
    {
        b.ToTable("Instruments");
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.HasOne<AcademicEvent>().WithMany().HasForeignKey(x => x.EventId);
        b.HasMany(x => x.Criteria).WithOne().HasForeignKey(c => c.InstrumentId);
    }
}

internal sealed class CriterionConfiguration : IEntityTypeConfiguration<Criterion>
{
    public void Configure(EntityTypeBuilder<Criterion> b)
    {
        b.ToTable("Criteria", t =>
        {
            t.HasCheckConstraint("CK_Criteria_Weight", "\"Weight\" > 0");
            t.HasCheckConstraint("CK_Criteria_Scale", "\"ScaleMax\" > \"ScaleMin\"");
        });
        b.Property(x => x.OptionsJson).HasColumnType("jsonb");
    }
}

internal sealed class ResponseConfiguration : IEntityTypeConfiguration<Response>
{
    public void Configure(EntityTypeBuilder<Response> b)
    {
        b.ToTable("Responses");
        b.HasOne<Criterion>().WithMany().HasForeignKey(x => x.CriterionId);
        b.HasOne<Work>().WithMany().HasForeignKey(x => x.WorkId);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.EvaluatorId);
        b.HasIndex(x => new { x.CriterionId, x.WorkId, x.EvaluatorId }).IsUnique();
    }
}

internal sealed class WorkResultConfiguration : IEntityTypeConfiguration<WorkResult>
{
    public void Configure(EntityTypeBuilder<WorkResult> b)
    {
        b.ToTable("WorkResults");
        b.HasKey(x => x.WorkId);
        b.HasOne<Work>().WithOne().HasForeignKey<WorkResult>(x => x.WorkId);
    }
}
