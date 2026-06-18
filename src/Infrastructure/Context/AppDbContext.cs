using Core.Domain.Entities;
using Core.Domain.FluxoCaixa;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Context;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Lancamento> Lancamentos => Set<Lancamento>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Lancamento>(e =>
        {
            e.ToTable("lancamentos");
            e.HasKey(x => x.Id);

            e.Property(x => x.Descricao)
             .HasMaxLength(200)
             .IsRequired();

            e.Property(x => x.Valor)
             .HasColumnType("numeric(18,2)");

            e.Property(x => x.Data)
             .HasColumnType("date")
             .IsRequired();
        });

        modelBuilder.Entity<SaldoDiario>(e =>
        {
            e.ToTable("saldos_diarios");

            e.HasKey(x => x.Dia);

            e.Property(x => x.Dia)
             .HasColumnType("date")   // ✅ DateOnly como PK
             .IsRequired();

            e.Property(x => x.Saldo)
             .HasColumnType("numeric(18,2)");
        });
    }
}