using System.Data;
using PipeFlow.Core;

namespace RealQuery.Core.Services;

/// <summary>
/// Serviço para manipular dados do SQL Server usando PipeFlowCore
/// </summary>
public class SqlServerDataService : IDataService
{
  public string[] SupportedExtensions => new[] { ".sql" }; // Placeholder

  public bool CanHandle(string filePath)
  {
    // SQL Server usa connection strings, não file paths
    return false;
  }

  /// <summary>
  /// Importa dados de uma query SQL Server
  /// </summary>
  public async Task<DataTable> ImportFromQueryAsync(
    string connectionString,
    string query)
  {
    if (string.IsNullOrEmpty(connectionString))
      throw new ArgumentNullException(nameof(connectionString));

    if (string.IsNullOrEmpty(query))
      throw new ArgumentNullException(nameof(query));

    await Task.CompletedTask;

    // Usar PipeFlow para ler do SQL Server
    var pipeline = PipeFlow.Core.PipeFlow.From.Sql(connectionString, query);
    var pipeFlowRows = pipeline.Execute();

    return PipeFlowDataBridge.ToDataTable(pipeFlowRows);
  }

  /// <summary>
  /// Importa dados de uma tabela SQL Server
  /// </summary>
  public async Task<DataTable> ImportFromTableAsync(
    string connectionString,
    string tableName)
  {
    var query = $"SELECT * FROM [{tableName}]";
    return await ImportFromQueryAsync(connectionString, query);
  }

  /// <summary>
  /// Exporta dados para uma tabela SQL Server
  /// </summary>
  public async Task ExportToTableAsync(
    DataTable data,
    string connectionString,
    string tableName,
    bool createIfNotExists = true)
  {
    if (data == null)
      throw new ArgumentNullException(nameof(data));

    if (string.IsNullOrEmpty(connectionString))
      throw new ArgumentNullException(nameof(connectionString));

    if (string.IsNullOrEmpty(tableName))
      throw new ArgumentNullException(nameof(tableName));

    await Task.CompletedTask;

    // Converter DataTable para PipeFlow.Core.DataRow
    var pipeFlowRows = PipeFlowDataBridge.ToPipeFlowRows(data);

    // Criar pipeline e exportar para SQL Server
    var pipeline = PipeFlow.Core.PipeFlow.From.DataRows(pipeFlowRows);
    pipeline.ToSql(connectionString, tableName, writer => {
      writer.WithBatchSize(1000);
      writer.WithTransaction(true);
    });
  }

  // Métodos da interface IDataService (não usados para SQL Server)
  public Task<DataTable> ImportAsync(string filePath)
    => throw new NotSupportedException("Use ImportFromQueryAsync ou ImportFromTableAsync para SQL Server.");

  public Task ExportAsync(DataTable data, string filePath)
    => throw new NotSupportedException("Use ExportToTableAsync para SQL Server.");
}
