using Core.Domain.Enums;

namespace Core.Domain.Entities;

public class Lancamento
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public decimal Valor { get; private set; }
    public string Descricao { get; private set; } = string.Empty;
    public DateOnly Data { get; private set; }
    public TipoLancamento Tipo { get; private set; }

    public Lancamento(decimal valor, string descricao, DateOnly data, TipoLancamento tipo)
    {
        if (valor <= 0)
            throw new ArgumentException("Valor deve ser positivo.", nameof(valor));

        if (string.IsNullOrWhiteSpace(descricao))
            throw new ArgumentException("Descrição é obrigatória.", nameof(descricao));

        Valor = valor;
        Descricao = descricao.Trim();
        Data = data;
        Tipo = tipo;
    }

    public static Lancamento Debito(decimal valor, string descricao, DateOnly data)
        => new(valor, descricao, data, TipoLancamento.Debito);

    public static Lancamento Credito(decimal valor, string descricao, DateOnly data)
        => new(valor, descricao, data, TipoLancamento.Credito);

    public decimal ValorEfetivo()
        => Tipo == TipoLancamento.Credito ? Valor : -Valor;
}