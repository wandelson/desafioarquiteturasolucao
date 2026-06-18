using Core.Application.Ports;
using Core.Domain.Entities;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.InMemory;

public class LancamentoRepository : ILancamentoRepository
{
    private readonly AppDbContext _context;

    public LancamentoRepository(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task SalvarAsync(Lancamento lanc)
    {
        await _context.Lancamentos.AddAsync(lanc);
        await _context.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<Lancamento>> ObterPorDiaAsync(DateOnly dia)
    {
        var res = await _context.Lancamentos
            .Where(l => l.Data.Day == dia.Day
                     && l.Data.Month == dia.Month
                     && l.Data.Year == dia.Year)
            .ToListAsync();

        return res;
    }
}