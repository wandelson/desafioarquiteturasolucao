using Core.Application;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class RelatoriosController : ControllerBase
{
    private readonly ObterRelatorioDiaHandler _handler;

    public RelatoriosController(ObterRelatorioDiaHandler handler)
    {
        _handler = handler;
    }

    [HttpGet("{dia}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ObterRelatorioDia(DateOnly dia)
    {
        var rel = await _handler.Handle(dia);

        if (rel is null)
            return NoContent();

        return Ok(rel);
    }
}
