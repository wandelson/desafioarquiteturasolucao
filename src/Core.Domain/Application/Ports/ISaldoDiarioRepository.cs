using Core.Domain.FluxoCaixa;

namespace Core.Application.Ports;

public interface ISaldoDiarioRepository
{
    Task<SaldoDiario?> ObterAsync(DateOnly dia);

    Task SalvarAsync(SaldoDiario saldo);
}