using Core.Application.Commands;
using Core.Application.Ports;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Lancamentos.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LancamentosController : ControllerBase
    {
        private readonly IRegistrarLancamentoHandler _handler;
        private readonly IValidator<RegistrarLancamentoCommand> _validator;

        public LancamentosController(
            IRegistrarLancamentoHandler handler,
            IValidator<RegistrarLancamentoCommand> validator)
        {
            _handler = handler;
            _validator = validator;
        }


        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Post([FromBody] RegistrarLancamentoCommand command)
        {
            var validationResult = await _validator.ValidateAsync(command);
            if (!validationResult.IsValid)
            {
                var details = new ValidationProblemDetails(validationResult.ToDictionary());
                return ValidationProblem(details);
            }

            var id = await _handler.Handle(command);
            return Created();
        }
    }
}