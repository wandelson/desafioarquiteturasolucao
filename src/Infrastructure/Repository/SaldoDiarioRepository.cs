using Core.Application.Ports;
using Core.Domain.FluxoCaixa;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repository
{
    public class SaldoDiarioRepository : ISaldoDiarioRepository
    {
        private readonly AppDbContext _context;

        public SaldoDiarioRepository(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<SaldoDiario?> ObterAsync(DateOnly dia)
        {
            return await _context.Set<SaldoDiario>()
                                 .AsNoTracking()
                                 .FirstOrDefaultAsync(s => s.Dia == dia)
                                 .ConfigureAwait(false);
        }

        public async Task SalvarAsync(SaldoDiario saldo)
        {
            if (saldo is null) throw new ArgumentNullException(nameof(saldo));

            saldo.Dia = saldo.Dia;

            var set = _context.Set<SaldoDiario>();
            var existing = await set
                .FirstOrDefaultAsync(s => s.Dia == saldo.Dia)
                .ConfigureAwait(false);

            if (existing is null)
            {
                set.Add(saldo);
            }
            else
            {
                _context.Entry(existing).CurrentValues.SetValues(saldo);
            }

            await _context.SaveChangesAsync().ConfigureAwait(false);
        }
    }
}