using Core.Application.Ports;

namespace Core.Application;

public class RelatorioDiaDto
{
    public DateOnly Dia { get; set; }
    public decimal Saldo { get; set; }
}

public class ObterRelatorioDiaHandler
{
    private readonly ISaldoDiarioRepository _repo;

    public ObterRelatorioDiaHandler(ISaldoDiarioRepository repo)
    {
        _repo = repo;
    }

    public async Task<RelatorioDiaDto?> Handle(DateOnly dia)
    {
        var saldo = await _repo.ObterAsync(dia);

        if (saldo is null)
            return null;

        return new RelatorioDiaDto
        {
            Dia = saldo.Dia,
            Saldo = saldo.Saldo
        };
    }
}