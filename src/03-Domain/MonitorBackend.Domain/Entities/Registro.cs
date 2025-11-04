namespace MonitorBackend.Domain.Entities;

/// <summary>
/// Entidade principal do domínio representando um registro de monitoramento.
/// Segue princípios de Domain-Driven Design.
/// </summary>
public class Registro
{
    public string Observacao { get; set; } = string.Empty;
    public DateTime DataHora { get; set; }
    public int Quantidade { get; set; }

    // Construtor para Dapper
    public Registro() { }

    public Registro(string observacao, DateTime dataHora, int quantidade)
    {
        Observacao = observacao;
        DataHora = dataHora;
        Quantidade = quantidade;
    }
}
