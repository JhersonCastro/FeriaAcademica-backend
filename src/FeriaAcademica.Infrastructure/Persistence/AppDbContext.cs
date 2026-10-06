using FeriaAcademica.Application.Abstractions;
using FeriaAcademica.Domain.Eventos;
using FeriaAcademica.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FeriaAcademica.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser>(options), IUnitOfWork
{
    public DbSet<Evento> Eventos => Set<Evento>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b); // tablas de Identity (AspNetUsers, AspNetRoles, AspNetUserLogins...)
        b.Entity<Evento>(e =>
        {
            e.ToTable("Eventos");
            e.HasKey(x => x.Id);
            e.Property(x => x.Titulo).HasMaxLength(200).IsRequired();
            e.Property(x => x.Estado).HasConversion<string>().HasMaxLength(30);
            e.Ignore(x => x.EventosDeDominio);
        });
    }

    Task<int> IUnitOfWork.SaveChangesAsync(CancellationToken ct) => SaveChangesAsync(ct);
}
