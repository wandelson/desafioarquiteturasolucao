using Core.Domain.Enums;

namespace Core.Application.Results;

public class RegistrarLancamentoResult
{
    public Guid Id { get; set; }
    public decimal Valor { get; set; }
    public DateTime Data { get; set; }
    public TipoLancamento Tipo { get; set; }
}