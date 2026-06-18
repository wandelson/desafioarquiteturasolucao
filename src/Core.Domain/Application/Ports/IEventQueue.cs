using Core.Domain.Events;

namespace Core.Application.Ports;

public interface IEventQueue
{
    Task PublicarAsync(LancamentoCriadoEvent evento);
}