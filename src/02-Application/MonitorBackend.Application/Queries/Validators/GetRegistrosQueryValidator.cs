using FluentValidation;

namespace MonitorBackend.Application.Queries.Validators;

/// <summary>
/// Validador para GetRegistrosQuery usando FluentValidation.
/// Define regras de validação para os parâmetros da query.
/// </summary>
public sealed class GetRegistrosQueryValidator : AbstractValidator<GetRegistrosQuery>
{
    public GetRegistrosQueryValidator()
    {
        // Atualmente não há parâmetros para validar
        // Mas a estrutura está pronta para expansão

        // Exemplos de regras futuras:
        // RuleFor(x => x.DataInicio)
        //     .LessThan(x => x.DataFim)
        //     .When(x => x.DataInicio.HasValue && x.DataFim.HasValue)
        //     .WithMessage("Data início deve ser anterior à data fim");

        // RuleFor(x => x.Limite)
        //     .GreaterThan(0)
        //     .LessThanOrEqualTo(1000)
        //     .When(x => x.Limite.HasValue)
        //     .WithMessage("Limite deve estar entre 1 e 1000");
    }
}
