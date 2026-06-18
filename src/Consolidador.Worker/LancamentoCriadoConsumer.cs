using Core.Application;
using Core.Domain.Events;
using MassTransit;

public class LancamentoCriadoConsumer : IConsumer<LancamentoCriadoEvent>
{
    private readonly ConsolidarDiaHandler _handler;

    public LancamentoCriadoConsumer(ConsolidarDiaHandler handler)
    {
        _handler = handler;
    }

    public async Task Consume(ConsumeContext<LancamentoCriadoEvent> context)
    {
        var msg = context.Message;

        Console.WriteLine($"[Worker] Evento recebido: {msg.LancamentoId} - Dia {msg.Dia:dd/MM/yyyy}");

        await _handler.ProcessarDia(msg.Dia);
    }
}