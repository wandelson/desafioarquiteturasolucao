using Core.Application.Ports;
using Core.Domain.FluxoCaixa;

namespace Core.Application;

public class ConsolidarDiaHandler
{
    private readonly ILancamentoRepository _lancRepo;
    private readonly ISaldoDiarioRepository _saldoRepo;

    public ConsolidarDiaHandler(
        ILancamentoRepository lancRepo,
        ISaldoDiarioRepository saldoRepo)
    {
        _lancRepo = lancRepo;
        _saldoRepo = saldoRepo;
    }

    public async Task ProcessarDia(DateOnly dia)
    {
        // Obtém todos os lançamentos do dia
        var lancamentos = await _lancRepo.ObterPorDiaAsync(dia);

        // Calcula o saldo consolidado
        var saldo = lancamentos.Sum(l => l.ValorEfetivo());

        // Cria a view materializada
        var saldoDiario = new SaldoDiario(dia, saldo);

        // Persiste a view
        await _saldoRepo.SalvarAsync(saldoDiario);
    }
}