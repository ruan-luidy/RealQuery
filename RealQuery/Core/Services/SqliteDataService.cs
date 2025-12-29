using System.Data;
using PipeFlow.Core;
using Microsoft.Data.Sqlite;

namespace RealQuery.Core.Services;

/// <summary>
/// Serviço para manipular arquivos SQLite usando PipeFlowCore
/// </summary>
public class SqliteDataService : IDataService
{
  public string[] SupportedExtensions => new[] { ".db", ".sqlite", ".sqlite3" };

  public bool CanHandle(string filePath)
  {
    if (string.IsNullOrEmpty(filePath))
      return false;

    var extension = Path.GetExtension(filePath).ToLowerInvariant();
    return SupportedExtensions.Contains(extension);
  }

  /// <summary>
  /// Importa dados de um arquivo SQLite (primeira tabela encontrada)
  /// </summary>
  public async Task<DataTable> ImportAsync(string filePath)
  {
    if (!File.Exists(filePath))
      throw new FileNotFoundException($"Arquivo não encontrado: {filePath}");

    if (!CanHandle(filePath))
      throw new NotSupportedException($"Formato de arquivo não suportado: {Path.GetExtension(filePath)}");

    await Task.CompletedTask;

    // Obter primeira tabela
    var tables = await GetTablesAsync(filePath);
    if (tables.Length == 0)
      throw new InvalidOperationException("Nenhuma tabela encontrada no arquivo SQLite.");

    // Importar primeira tabela
    return await ImportFromTableAsync(filePath, tables[0]);
  }

  /// <summary>
  /// Importa dados de uma tabela específica do SQLite
  /// </summary>
  public async Task<DataTable> ImportFromTableAsync(string filePath, string tableName)
  {
    if (!File.Exists(filePath))
      throw new FileNotFoundException($"Arquivo não encontrado: {filePath}");

    if (string.IsNullOrEmpty(tableName))
      throw new ArgumentNullException(nameof(tableName));

    await Task.CompletedTask;

    var connectionString = $"Data Source={filePath}";
    var query = $"SELECT * FROM [{tableName}]";

    // Usar SQLite diretamente
    var dataTable = new System.Data.DataTable(tableName);

    using var connection = new SqliteConnection(connectionString);
    await connection.OpenAsync();

    using var command = connection.CreateCommand();
    command.CommandText = query;

    using var reader = await command.ExecuteReaderAsync();

    // Criar esquema do DataTable
    for (int i = 0; i < reader.FieldCount; i++)
    {
      var columnName = reader.GetName(i);
      var columnType = reader.GetFieldType(i);
      dataTable.Columns.Add(columnName, columnType);
    }

    // Ler dados
    while (await reader.ReadAsync())
    {
      var row = dataTable.NewRow();
      for (int i = 0; i < reader.FieldCount; i++)
      {
        row[i] = reader.IsDBNull(i) ? DBNull.Value : reader.GetValue(i);
      }
      dataTable.Rows.Add(row);
    }

    return dataTable;
  }

  /// <summary>
  /// Exporta dados para um arquivo SQLite
  /// </summary>
  public async Task ExportAsync(DataTable data, string filePath)
  {
    if (data == null)
      throw new ArgumentNullException(nameof(data));

    if (string.IsNullOrEmpty(filePath))
      throw new ArgumentNullException(nameof(filePath));

    if (!CanHandle(filePath))
      throw new NotSupportedException($"Formato de arquivo não suportado: {Path.GetExtension(filePath)}");

    await Task.CompletedTask;

    var connectionString = $"Data Source={filePath}";
    var tableName = string.IsNullOrEmpty(data.TableName) ? "data" : data.TableName;

    using var connection = new SqliteConnection(connectionString);
    await connection.OpenAsync();

    // Criar tabela se não existir
    var createTableSql = GenerateCreateTableSql(tableName, data);
    using (var createCommand = connection.CreateCommand())
    {
      createCommand.CommandText = createTableSql;
      await createCommand.ExecuteNonQueryAsync();
    }

    // Inserir dados
    var insertSql = GenerateInsertSql(tableName, data);
    using (var transaction = connection.BeginTransaction())
    {
      foreach (System.Data.DataRow row in data.Rows)
      {
        using var insertCommand = connection.CreateCommand();
        insertCommand.CommandText = insertSql;

        for (int i = 0; i < data.Columns.Count; i++)
        {
          var value = row[i];
          insertCommand.Parameters.AddWithValue($"@p{i}", value == DBNull.Value ? null : value);
        }

        await insertCommand.ExecuteNonQueryAsync();
      }

      await transaction.CommitAsync();
    }
  }

  /// <summary>
  /// Obtém lista de tabelas no arquivo SQLite
  /// </summary>
  public async Task<string[]> GetTablesAsync(string filePath)
  {
    if (!File.Exists(filePath))
      throw new FileNotFoundException($"Arquivo não encontrado: {filePath}");

    await Task.CompletedTask;

    var connectionString = $"Data Source={filePath}";
    var tables = new List<string>();

    using var connection = new SqliteConnection(connectionString);
    await connection.OpenAsync();

    using var command = connection.CreateCommand();
    command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'";

    using var reader = await command.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
      tables.Add(reader.GetString(0));
    }

    return tables.ToArray();
  }

  /// <summary>
  /// Gera SQL para criar tabela SQLite
  /// </summary>
  private string GenerateCreateTableSql(string tableName, System.Data.DataTable data)
  {
    var columns = new List<string>();

    foreach (System.Data.DataColumn column in data.Columns)
    {
      var sqliteType = GetSqliteType(column.DataType);
      columns.Add($"[{column.ColumnName}] {sqliteType}");
    }

    return $"CREATE TABLE IF NOT EXISTS [{tableName}] ({string.Join(", ", columns)})";
  }

  /// <summary>
  /// Gera SQL para inserir dados
  /// </summary>
  private string GenerateInsertSql(string tableName, System.Data.DataTable data)
  {
    var columnNames = new List<string>();
    var parameters = new List<string>();

    for (int i = 0; i < data.Columns.Count; i++)
    {
      columnNames.Add($"[{data.Columns[i].ColumnName}]");
      parameters.Add($"@p{i}");
    }

    return $"INSERT INTO [{tableName}] ({string.Join(", ", columnNames)}) VALUES ({string.Join(", ", parameters)})";
  }

  /// <summary>
  /// Mapeia tipos .NET para tipos SQLite
  /// </summary>
  private string GetSqliteType(Type type)
  {
    if (type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte) || type == typeof(bool))
      return "INTEGER";

    if (type == typeof(float) || type == typeof(double) || type == typeof(decimal))
      return "REAL";

    if (type == typeof(DateTime))
      return "TEXT"; // SQLite não tem tipo DATE nativo

    if (type == typeof(byte[]))
      return "BLOB";

    return "TEXT"; // Default para string e outros
  }
}
