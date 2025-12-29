namespace RealQuery.Core.Models;

/// <summary>
/// Informações sobre conexão de banco de dados
/// </summary>
public class DatabaseConnectionInfo
{
  public string Name { get; set; } = "";
  public DatabaseType DatabaseType { get; set; }
  public string ConnectionString { get; set; } = "";
  public string Query { get; set; } = "SELECT * FROM ";
  public string FilePath { get; set; } = ""; // Para SQLite
  public string Server { get; set; } = "localhost";
  public string Database { get; set; } = "";
  public string Username { get; set; } = "";
  public string Password { get; set; } = "";
  public string TableName { get; set; } = "";

  /// <summary>
  /// Constrói a connection string baseada no tipo de banco
  /// </summary>
  public string BuildConnectionString()
  {
    return DatabaseType switch
    {
      DatabaseType.SqlServer => $"Server={Server};Database={Database};User Id={Username};Password={Password};",
      DatabaseType.Sqlite => $"Data Source={FilePath}",
      _ => ConnectionString
    };
  }
}

/// <summary>
/// Tipos de banco de dados suportados
/// </summary>
public enum DatabaseType
{
  SqlServer,
  Sqlite
}
