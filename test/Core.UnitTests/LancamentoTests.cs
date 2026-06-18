using Core.Domain.Entities;
using Core.Domain.Enums;

namespace Domain.UnitTests.Lancamentos.RegistrarLancamento;

public class LancamentoTests
{
    [Fact]
    public void Deve_criar_entrada_de_dinheiro_credito()
    {
        var lancamento = Lancamento.Credito(
            valor: 150m,
            descricao: "Venda de produto",
            data: new DateOnly(2025, 1, 10)
        );

        Assert.Equal(150m, lancamento.Valor);
        Assert.Equal("Venda de produto", lancamento.Descricao);
        Assert.Equal(TipoLancamento.Credito, lancamento.Tipo);
        Assert.Equal(150m, lancamento.ValorEfetivo()); // aumenta saldo
    }

    [Fact]
    public void Deve_criar_saida_de_dinheiro_debito()
    {
        var lancamento = Lancamento.Debito(
            valor: 80m,
            descricao: "Compra de mercadoria",
            data: new DateOnly(2025, 1, 10)
        );

        Assert.Equal(80m, lancamento.Valor);
        Assert.Equal("Compra de mercadoria", lancamento.Descricao);
        Assert.Equal(TipoLancamento.Debito, lancamento.Tipo);
        Assert.Equal(-80m, lancamento.ValorEfetivo()); // reduz saldo
    }

    [Fact]
    public void Nao_deve_permitir_valor_zero_ou_negativo()
    {
        Assert.Throws<ArgumentException>(() =>
            Lancamento.Credito(0, "Venda", DateOnly.FromDateTime(DateTime.Today)));

        Assert.Throws<ArgumentException>(() =>
            Lancamento.Debito(-10, "Despesa", DateOnly.FromDateTime(DateTime.UtcNow)));
    }

    [Fact]
    public void Nao_deve_permitir_descricao_vazia()
    {
        Assert.Throws<ArgumentException>(() =>
            Lancamento.Credito(100, "", DateOnly.FromDateTime(DateTime.UtcNow)));
    }
}