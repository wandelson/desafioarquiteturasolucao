using Core.Domain.Enums;

namespace Core.Domain.Events;

public class LancamentoCriadoEvent
{
    public Guid LancamentoId { get; set; }
    public DateOnly Dia { get; set; }
    public decimal Valor { get; set; }
    public TipoLancamento Tipo { get; set; }
}