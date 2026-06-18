using Core.Application.Commands;
using Core.Application.Ports;
using Core.Domain.Entities;
using Core.Domain.Events;

namespace Core.Application.Handlers;

public class RegistrarLancamentoHandler : IRegistrarLancamentoHandler
{
    private readonly ILancamentoRepository _repo;
    private readonly IEventQueue _eventQueue;

    public RegistrarLancamentoHandler(
        ILancamentoRepository repo,
        IEventQueue eventQueue)
    {
        _repo = repo;
        _eventQueue = eventQueue;
    }

    public async Task<Guid> Handle(RegistrarLancamentoCommand cmd)
    {
        if (cmd.Valor <= 0)
            throw new ArgumentException("Valor deve ser maior que zero.");

        var lancamento = new Lancamento(
            cmd.Valor,
            cmd.Descricao,
            cmd.Data,
            cmd.Tipo
        );

        await _repo.SalvarAsync(lancamento);

        var evento = new LancamentoCriadoEvent
        {
            LancamentoId = lancamento.Id,
            Dia = lancamento.Data,
            Valor = lancamento.Valor,
            Tipo = lancamento.Tipo
        };
        await _eventQueue.PublicarAsync(evento);

        return lancamento.Id;
    }
}