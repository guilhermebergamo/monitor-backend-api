using MonitorBackend.Application.Abstractions;
using MonitorBackend.Domain.Repositories;

namespace MonitorBackend.Application.Commands;

/// <summary>
/// Handler para registrar acesso do frontend
/// Sempre incrementa a quantidade em 1
/// </summary>
public class RegistrarAcessoCommandHandler : ICommandHandler<RegistrarAcessoCommand, RegistrarAcessoCommandResult>
{
    private readonly IRegistroRepository _repository;

    public RegistrarAcessoCommandHandler(IRegistroRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<RegistrarAcessoCommandResult> Handle(
        RegistrarAcessoCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validação básica
            if (string.IsNullOrWhiteSpace(command.Observacao))
            {
                return RegistrarAcessoCommandResult.CreateFailure("Observação não pode ser vazia");
            }

            // Data/hora atual
            var dataHora = DateTime.Now;

            // Busca a quantidade do último registro e incrementa
            var quantidadeUltimoRegistro = await _repository.GetUltimoRegistroAsync(cancellationToken);
            var novaQuantidade = quantidadeUltimoRegistro + 1;

            // Insere no banco
            var registroId = await _repository.InsertAsync(command.Observacao, dataHora, novaQuantidade);

            // Busca os registros paginados (primeira página, 10 registros)
            var (registros, totalCount, somaQuantidade) = await _repository.GetPaginatedAsync(1, 10, cancellationToken);

            return RegistrarAcessoCommandResult.CreateSuccess(registros, totalCount, somaQuantidade, 1, 10);
        }
        catch (Exception ex)
        {
            return RegistrarAcessoCommandResult.CreateFailure($"Erro ao registrar acesso: {ex.Message}");
        }
    }
}
