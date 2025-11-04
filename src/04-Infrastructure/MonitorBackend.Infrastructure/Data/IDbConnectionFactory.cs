using System.Data;
using Npgsql;

namespace MonitorBackend.Infrastructure.Data;

/// <summary>
/// Factory para criar conexões com o banco de dados PostgreSQL.
/// Implementa o padrão Factory para centralizar a criação de conexões.
/// </summary>
public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}

/// <summary>
/// Implementação concreta da factory de conexões PostgreSQL usando Npgsql.
/// </summary>
public sealed class NpgsqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public NpgsqlConnectionFactory(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string não pode ser nula ou vazia", nameof(connectionString));

        _connectionString = connectionString;
    }

    public IDbConnection CreateConnection()
    {
        return new NpgsqlConnection(_connectionString);
    }
}
