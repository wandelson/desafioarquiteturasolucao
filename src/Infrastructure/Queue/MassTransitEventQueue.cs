using Core.Application.Ports;
using Core.Domain.Events;
using MassTransit;

namespace Infrastructure.Queue;

public class MassTransitEventQueue : IEventQueue
{
    private readonly ISendEndpointProvider _send;

    public MassTransitEventQueue(ISendEndpointProvider publish)
    {
        _send = publish;
    }

    public async Task PublicarAsync(LancamentoCriadoEvent evento)
    {
        var endpoint = await _send.GetSendEndpoint(new Uri("queue:lancamento-criado"));

        await endpoint.Send(evento);
    }
}