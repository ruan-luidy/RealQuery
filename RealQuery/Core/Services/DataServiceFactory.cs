namespace RealQuery.Core.Services;

/// <summary>
/// Factory para criar instâncias de IDataService baseado no tipo de arquivo
/// </summary>
public class DataServiceFactory
{
  /// <summary>
  /// Obtém o serviço de dados apropriado para o arquivo
  /// </summary>
  public IDataService GetDataService(string filePath)
  {
    if (string.IsNullOrEmpty(filePath))
      throw new ArgumentNullException(nameof(filePath));

    var extension = Path.GetExtension(filePath).ToLowerInvariant();

    return extension switch
    {
      ".xlsx" or ".xls" or ".xlsm" => new PipeFlowExcelDataService(),
      ".csv" or ".txt" => new PipeFlowCsvDataService(),
      ".json" or ".jsonl" => new JsonDataService(),
      ".db" or ".sqlite" or ".sqlite3" => new SqliteDataService(),
      _ => throw new NotSupportedException($"Formato de arquivo não suportado: {extension}")
    };
  }

  /// <summary>
  /// Obtém o serviço para SQL Server
  /// </summary>
  public SqlServerDataService GetSqlServerService()
  {
    return new SqlServerDataService();
  }

  /// <summary>
  /// Obtém o serviço para SQLite
  /// </summary>
  public SqliteDataService GetSqliteService()
  {
    return new SqliteDataService();
  }

  /// <summary>
  /// Obtém todas as extensões suportadas
  /// </summary>
  public IEnumerable<string> GetSupportedExtensions()
  {
    return new[]
    {
      ".xlsx", ".xls", ".xlsm",      // Excel
      ".csv", ".txt",                 // CSV
      ".json", ".jsonl",              // JSON
      ".db", ".sqlite", ".sqlite3"    // SQLite
    };
  }

  /// <summary>
  /// Verifica se uma extensão é suportada
  /// </summary>
  public bool IsExtensionSupported(string filePath)
  {
    if (string.IsNullOrEmpty(filePath))
      return false;

    var extension = Path.GetExtension(filePath).ToLowerInvariant();
    return GetSupportedExtensions().Contains(extension);
  }
}
