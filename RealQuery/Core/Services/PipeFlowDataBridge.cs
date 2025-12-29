using System.Data;
using System.Dynamic;
using PipeFlow.Core;

namespace RealQuery.Core.Services;

/// <summary>
/// Bridge para converter dados entre PipeFlow e DataTable
/// </summary>
public static class PipeFlowDataBridge
{
  /// <summary>
  /// Converte IEnumerable de PipeFlow.Core.DataRow para System.Data.DataTable
  /// </summary>
  public static DataTable ToDataTable(IEnumerable<PipeFlow.Core.DataRow> pipeFlowRows)
  {
    if (pipeFlowRows == null)
      throw new ArgumentNullException(nameof(pipeFlowRows));

    var dataTable = new System.Data.DataTable();
    var isSchemaCreated = false;
    var rowsList = pipeFlowRows.ToList();

    if (rowsList.Count == 0)
      return dataTable;

    foreach (var pipeFlowRow in rowsList)
    {
      if (!isSchemaCreated)
      {
        CreateSchemaFromPipeFlowRow(dataTable, pipeFlowRow);
        isSchemaCreated = true;
      }

      var dataRow = dataTable.NewRow();
      PopulateRowFromPipeFlowRow(dataRow, pipeFlowRow, dataTable);
      dataTable.Rows.Add(dataRow);
    }

    return dataTable;
  }

  /// <summary>
  /// Converte dados do PipeFlow (IEnumerable&lt;dynamic&gt;) para DataTable (legacy)
  /// </summary>
  public static DataTable ToDataTable(IEnumerable<dynamic> pipeFlowData)
  {
    if (pipeFlowData == null)
      throw new ArgumentNullException(nameof(pipeFlowData));

    var dataTable = new DataTable();
    var isSchemaCreated = false;
    var recordsList = pipeFlowData.ToList();

    if (recordsList.Count == 0)
      return dataTable;

    foreach (var record in recordsList)
    {
      if (!isSchemaCreated)
      {
        CreateSchemaFromDynamic(dataTable, record);
        isSchemaCreated = true;
      }

      var row = dataTable.NewRow();
      PopulateRowFromDynamic(row, record, dataTable);
      dataTable.Rows.Add(row);
    }

    return dataTable;
  }

  /// <summary>
  /// Converte DataTable para formato PipeFlow (Dictionary)
  /// </summary>
  public static IEnumerable<Dictionary<string, object>> FromDataTable(System.Data.DataTable dataTable)
  {
    if (dataTable == null)
      throw new ArgumentNullException(nameof(dataTable));

    var result = new List<Dictionary<string, object>>();

    foreach (System.Data.DataRow row in dataTable.Rows)
    {
      var dict = new Dictionary<string, object>();

      foreach (System.Data.DataColumn column in dataTable.Columns)
      {
        var value = row[column];
        dict[column.ColumnName] = value == DBNull.Value ? null : value;
      }

      result.Add(dict);
    }

    return result;
  }

  /// <summary>
  /// Cria esquema do DataTable baseado no primeiro registro dinâmico
  /// </summary>
  private static void CreateSchemaFromDynamic(System.Data.DataTable dataTable, dynamic record)
  {
    IDictionary<string, object> properties;

    // Tentar converter para Dictionary primeiro
    if (record is IDictionary<string, object> dict)
    {
      properties = dict;
    }
    else if (record is ExpandoObject expando)
    {
      properties = expando;
    }
    else
    {
      // Usar reflexão para extrair propriedades
      properties = new Dictionary<string, object>();
      var type = record.GetType();
      foreach (var prop in type.GetProperties())
      {
        properties[prop.Name] = prop.GetValue(record);
      }
    }

    // Criar colunas baseadas nas propriedades
    var columnNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    foreach (var kvp in properties)
    {
      var columnName = kvp.Key;
      var value = kvp.Value;

      // Garantir nomes únicos de colunas
      var originalName = columnName;
      var counter = 1;
      while (columnNames.Contains(columnName))
      {
        columnName = $"{originalName}_{counter++}";
      }
      columnNames.Add(columnName);

      // Inferir tipo da coluna
      var columnType = InferClrType(value);
      dataTable.Columns.Add(columnName, columnType);
    }
  }

  /// <summary>
  /// Popula uma linha do DataTable com dados dinâmicos
  /// </summary>
  private static void PopulateRowFromDynamic(System.Data.DataRow row, dynamic record, System.Data.DataTable dataTable)
  {
    IDictionary<string, object> properties;

    // Converter para Dictionary
    if (record is IDictionary<string, object> dict)
    {
      properties = dict;
    }
    else if (record is ExpandoObject expando)
    {
      properties = expando;
    }
    else
    {
      properties = new Dictionary<string, object>();
      var type = record.GetType();
      foreach (var prop in type.GetProperties())
      {
        properties[prop.Name] = prop.GetValue(record);
      }
    }

    // Preencher colunas
    foreach (System.Data.DataColumn column in dataTable.Columns)
    {
      if (properties.TryGetValue(column.ColumnName, out var value))
      {
        row[column] = ConvertValue(value, column.DataType) ?? DBNull.Value;
      }
      else
      {
        row[column] = DBNull.Value;
      }
    }
  }

  /// <summary>
  /// Infere o tipo CLR de um valor
  /// </summary>
  private static Type InferClrType(object value)
  {
    if (value == null)
      return typeof(string); // Default para string

    var type = value.GetType();

    // Se é um tipo primitivo ou conhecido, usar diretamente
    if (type.IsPrimitive || type == typeof(string) || type == typeof(DateTime) ||
        type == typeof(decimal) || type == typeof(Guid) || type == typeof(TimeSpan))
    {
      return type;
    }

    // Tentar inferir de tipos nuláveis
    var underlyingType = Nullable.GetUnderlyingType(type);
    if (underlyingType != null)
    {
      return underlyingType;
    }

    // Default para string para tipos complexos
    return typeof(string);
  }

  /// <summary>
  /// Converte um valor para o tipo de destino
  /// </summary>
  private static object? ConvertValue(object value, Type targetType)
  {
    if (value == null)
      return null;

    // Se já é do tipo correto
    if (value.GetType() == targetType)
      return value;

    try
    {
      // Tratar tipos nuláveis
      var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

      // Conversões especiais
      if (underlyingType == typeof(DateTime))
      {
        if (value is string strValue)
        {
          if (DateTime.TryParse(strValue, out var dateTime))
            return dateTime;
        }
        return Convert.ToDateTime(value);
      }

      if (underlyingType == typeof(bool))
      {
        if (value is string boolStr)
        {
          if (bool.TryParse(boolStr, out var boolValue))
            return boolValue;
          // Aceitar 0/1
          if (int.TryParse(boolStr, out var intValue))
            return intValue != 0;
        }
        return Convert.ToBoolean(value);
      }

      if (underlyingType == typeof(Guid))
      {
        if (value is string guidStr && Guid.TryParse(guidStr, out var guid))
          return guid;
      }

      // Conversão genérica
      return Convert.ChangeType(value, underlyingType);
    }
    catch
    {
      // Se falhar, tentar converter para string
      return value?.ToString();
    }
  }

  /// <summary>
  /// Valida se dois DataTables são equivalentes (mesma estrutura e dados)
  /// </summary>
  public static bool AreDataTablesEquivalent(DataTable dt1, DataTable dt2)
  {
    if (dt1 == null || dt2 == null)
      return dt1 == dt2;

    // Validar estrutura
    if (dt1.Columns.Count != dt2.Columns.Count)
      return false;

    if (dt1.Rows.Count != dt2.Rows.Count)
      return false;

    // Validar nomes de colunas
    for (int i = 0; i < dt1.Columns.Count; i++)
    {
      if (!dt1.Columns[i].ColumnName.Equals(dt2.Columns[i].ColumnName, StringComparison.OrdinalIgnoreCase))
        return false;
    }

    // Validar dados
    for (int row = 0; row < dt1.Rows.Count; row++)
    {
      for (int col = 0; col < dt1.Columns.Count; col++)
      {
        var val1 = dt1.Rows[row][col];
        var val2 = dt2.Rows[row][col];

        // Ambos null ou DBNull
        if ((val1 == null || val1 == DBNull.Value) && (val2 == null || val2 == DBNull.Value))
          continue;

        // Um é null e outro não
        if ((val1 == null || val1 == DBNull.Value) != (val2 == null || val2 == DBNull.Value))
          return false;

        // Comparar valores
        if (!val1.Equals(val2))
          return false;
      }
    }

    return true;
  }

  /// <summary>
  /// Converte System.Data.DataTable para IEnumerable de PipeFlow.Core.DataRow
  /// </summary>
  public static IEnumerable<PipeFlow.Core.DataRow> ToPipeFlowRows(System.Data.DataTable dataTable)
  {
    if (dataTable == null)
      throw new ArgumentNullException(nameof(dataTable));

    foreach (System.Data.DataRow row in dataTable.Rows)
    {
      var dict = new Dictionary<string, object>();

      foreach (System.Data.DataColumn column in dataTable.Columns)
      {
        var value = row[column];
        dict[column.ColumnName] = value == DBNull.Value ? null : value;
      }

      yield return new PipeFlow.Core.DataRow(dict);
    }
  }

  /// <summary>
  /// Cria esquema do DataTable baseado em PipeFlow.Core.DataRow
  /// </summary>
  private static void CreateSchemaFromPipeFlowRow(System.Data.DataTable dataTable, PipeFlow.Core.DataRow pipeFlowRow)
  {
    var columnNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    foreach (var columnName in pipeFlowRow.GetColumnNames())
    {
      var originalName = columnName;
      var uniqueName = columnName;
      var counter = 1;

      // Garantir nomes únicos
      while (columnNames.Contains(uniqueName))
      {
        uniqueName = $"{originalName}_{counter++}";
      }
      columnNames.Add(uniqueName);

      // Inferir tipo da coluna
      var value = pipeFlowRow[columnName];
      var columnType = InferClrType(value);
      dataTable.Columns.Add(uniqueName, columnType);
    }
  }

  /// <summary>
  /// Popula uma linha do DataTable com dados do PipeFlow.Core.DataRow
  /// </summary>
  private static void PopulateRowFromPipeFlowRow(System.Data.DataRow dataRow, PipeFlow.Core.DataRow pipeFlowRow, System.Data.DataTable dataTable)
  {
    foreach (System.Data.DataColumn column in dataTable.Columns)
    {
      if (pipeFlowRow.ContainsColumn(column.ColumnName))
      {
        var value = pipeFlowRow[column.ColumnName];
        dataRow[column] = ConvertValue(value, column.DataType) ?? DBNull.Value;
      }
      else
      {
        dataRow[column] = DBNull.Value;
      }
    }
  }
}
