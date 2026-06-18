using Core.Application.Commands;

namespace Core.Application.Ports;

public interface IRegistrarLancamentoHandler
{
    public Task<Guid> Handle(RegistrarLancamentoCommand cmd);
}