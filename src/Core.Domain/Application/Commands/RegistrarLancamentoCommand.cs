using Core.Domain.Enums;

namespace Core.Application.Commands;

public class RegistrarLancamentoCommand
{
    public decimal Valor { get; set; }
    public string Descricao { get; set; }
    public DateOnly Data { get; set; }
    public TipoLancamento Tipo { get; set; }
}