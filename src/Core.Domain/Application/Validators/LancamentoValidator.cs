using Core.Application.Commands;
using FluentValidation;

public class LancamentoValidator : AbstractValidator<RegistrarLancamentoCommand>
{
    public LancamentoValidator()
    {
        RuleFor(x => x.Descricao)
            .NotEmpty()
            .WithMessage("Descrição é obrigatória");

        RuleFor(x => x.Valor)
            .GreaterThan(0)
            .WithMessage("Valor deve ser maior que zero");

        RuleFor(x => x.Data)
            .NotEmpty()
            .WithMessage("Data é obrigatória");
    }
}