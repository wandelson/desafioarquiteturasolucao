using Core.Domain.Entities;

namespace Core.Domain.FluxoCaixa;

public class SaldoDiario
{
    public DateOnly Dia { get; set; }
    public decimal Saldo { get; set; }

    public SaldoDiario(DateOnly dia, decimal saldo)
    {
        Dia = dia;
        Saldo = saldo;
    }

    public void AplicarLancamento(Lancamento lancamento)
    {
        Saldo += lancamento.ValorEfetivo();
    }
}