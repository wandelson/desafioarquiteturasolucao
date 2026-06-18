using Core.Domain.Events;

public interface IConsolidarSaldoHandler
{
    Task Handle(LancamentoCriadoEvent @event, CancellationToken ct);
}