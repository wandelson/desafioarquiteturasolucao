using Core.Domain.Entities;

namespace Core.Application.Ports;

public interface ILancamentoRepository
{
    Task SalvarAsync(Lancamento lancamento);

    Task<IReadOnlyList<Lancamento>> ObterPorDiaAsync(DateOnly dia);
}