using System.Data;
using System.Text;

namespace RealQuery.Core.Services;

/// <summary>
/// Serviço para manipular arquivos CSV
/// </summary>
public class CsvDataService : IDataService
{
  public string[] SupportedExtensions => new[] { ".csv", ".txt" };

  public bool CanHandle(string filePath)
  {
    if (string.IsNullOrEmpty(filePath))
      return false;

    var extension = Path.GetExtension(filePath).ToLowerInvariant();
    return SupportedExtensions.Contains(extension);
  }

  /// <summary>
  /// Importa dados de um arquivo CSV
  /// </summary>
  public async Task<DataTable> ImportAsync(string filePath)
  {
    if (!File.Exists(filePath))
      throw new FileNotFoundException($"Arquivo não encontrado: {filePath}");

    if (!CanHandle(filePath))
      throw new NotSupportedException($"Formato de arquivo não suportado: {Path.GetExtension(filePath)}");

    return await Task.Run(() =>
    {
      try
      {
        return ParseCsv(filePath);
      }
      catch (Exception ex)
      {
        throw new InvalidOperationException($"Erro ao importar CSV: {ex.Message}", ex);
      }
    });
  }

  /// <summary>
  /// Exporta dados para um arquivo CSV
  /// </summary>
  public async Task ExportAsync(DataTable data, string filePath)
  {
    if (data == null)
      throw new ArgumentNullException(nameof(data));

    if (string.IsNullOrEmpty(filePath))
      throw new ArgumentNullException(nameof(filePath));

    if (!CanHandle(filePath))
      throw new NotSupportedException($"Formato de arquivo não suportado: {Path.GetExtension(filePath)}");

    await Task.Run(() =>
    {
      try
      {
        WriteCsv(data, filePath);
      }
      catch (Exception ex)
      {
        throw new InvalidOperationException($"Erro ao exportar CSV: {ex.Message}", ex);
      }
    });
  }

  /// <summary>
  /// Importa CSV com opções customizadas
  /// </summary>
  public async Task<DataTable> ImportAsync(string filePath, CsvOptions options)
  {
    if (!File.Exists(filePath))
      throw new FileNotFoundException($"Arquivo não encontrado: {filePath}");

    return await Task.Run(() =>
    {
      try
      {
        return ParseCsv(filePath, options);
      }
      catch (Exception ex)
      {
        throw new InvalidOperationException($"Erro ao importar CSV: {ex.Message}", ex);
      }
    });
  }

  /// <summary>
  /// Exporta CSV com opções customizadas
  /// </summary>
  public async Task ExportAsync(DataTable data, string filePath, CsvOptions options)
  {
    if (data == null)
      throw new ArgumentNullException(nameof(data));

    if (string.IsNullOrEmpty(filePath))
      throw new ArgumentNullException(nameof(filePath));

    await Task.Run(() =>
    {
      try
      {
        WriteCsv(data, filePath, options);
      }
      catch (Exception ex)
      {
        throw new InvalidOperationException($"Erro ao exportar CSV: {ex.Message}", ex);
      }
    });
  }

  /// <summary>
  /// Detecta automaticamente o delimitador do CSV
  /// </summary>
  public async Task<char> DetectDelimiterAsync(string filePath)
  {
    if (!File.Exists(filePath))
      throw new FileNotFoundException($"Arquivo não encontrado: {filePath}");

    return await Task.Run(() =>
    {
      try
      {
        var sampleLines = File.ReadLines(filePath, Encoding.UTF8).Take(5).ToArray();
        if (sampleLines.Length == 0)
          return ',';

        var delimiters = new[] { ',', ';', '\t', '|' };
        var counts = new Dictionary<char, int>();

        foreach (var delimiter in delimiters)
        {
          var totalCount = sampleLines.Sum(line => line.Count(c => c == delimiter));
          counts[delimiter] = totalCount;
        }

        return counts.OrderByDescending(kvp => kvp.Value).First().Key;
      }
      catch
      {
        return ','; // Default fallback
      }
    });
  }

  /// <summary>
  /// Faz parse do arquivo CSV
  /// </summary>
  private DataTable ParseCsv(string filePath, CsvOptions? options = null)
  {
    options ??= new CsvOptions();

    var dataTable = new DataTable();
    var lines = File.ReadAllLines(filePath, options.Encoding);

    if (lines.Length == 0)
      return dataTable;

    // Detectar delimitador se não especificado
    if (options.Delimiter == '\0')
    {
      options.Delimiter = DetectDelimiter(lines);
    }

    // Processar header
    var headerLine = lines[0];
    var headers = SplitCsvLine(headerLine, options.Delimiter, options.TextQualifier);

    if (options.HasHeaders)
    {
      // Usar primeira linha como headers
      foreach (var header in headers)
      {
        var columnName = string.IsNullOrWhiteSpace(header) ? $"Column{dataTable.Columns.Count + 1}" : header.Trim();

        // Garantir nomes únicos
        var originalName = columnName;
        var counter = 1;
        while (dataTable.Columns.Contains(columnName))
        {
          columnName = $"{originalName}_{counter++}";
        }

        dataTable.Columns.Add(columnName);
      }
    }
    else
    {
      // Gerar nomes de colunas automáticos
      for (int i = 0; i < headers.Length; i++)
      {
        dataTable.Columns.Add($"Column{i + 1}");
      }

      // Adicionar primeira linha como dados
      AddDataRow(dataTable, headers);
    }

    // Processar linhas de dados
    var startIndex = options.HasHeaders ? 1 : 1;
    for (int i = startIndex; i < lines.Length; i++)
    {
      var line = lines[i].Trim();

      if (string.IsNullOrEmpty(line) && options.SkipEmptyLines)
        continue;

      var values = SplitCsvLine(line, options.Delimiter, options.TextQualifier);
      AddDataRow(dataTable, values);
    }

    return dataTable;
  }

  /// <summary>
  /// Escreve DataTable para arquivo CSV
  /// </summary>
  private void WriteCsv(DataTable data, string filePath, CsvOptions? options = null)
  {
    options ??= new CsvOptions();

    using var writer = new StreamWriter(filePath, false, options.Encoding);

    // Escrever headers se necessário
    if (options.HasHeaders)
    {
      var headers = data.Columns.Cast<DataColumn>()
          .Select(col => EscapeCsvValue(col.ColumnName, options.Delimiter, options.TextQualifier));
      writer.WriteLine(string.Join(options.Delimiter, headers));
    }

    // Escrever dados
    foreach (DataRow row in data.Rows)
    {
      var values = row.ItemArray
          .Select(value => EscapeCsvValue(value?.ToString() ?? "", options.Delimiter, options.TextQualifier));
      writer.WriteLine(string.Join(options.Delimiter, values));
    }
  }

  /// <summary>
  /// Detecta delimitador automaticamente
  /// </summary>
  private char DetectDelimiter(string[] lines)
  {
    if (lines.Length == 0)
      return ',';

    var delimiters = new[] { ',', ';', '\t', '|' };
    var counts = new Dictionary<char, int>();

    var sampleLines = lines.Take(Math.Min(5, lines.Length));

    foreach (var delimiter in delimiters)
    {
      var totalCount = sampleLines.Sum(line => line.Count(c => c == delimiter));
      counts[delimiter] = totalCount;
    }

    return counts.OrderByDescending(kvp => kvp.Value).First().Key;
  }

  /// <summary>
  /// Divide linha CSV respeitando qualificadores de texto
  /// </summary>
  private string[] SplitCsvLine(string line, char delimiter, char textQualifier)
  {
    var result = new List<string>();
    var current = new StringBuilder();
    var insideQuotes = false;

    for (int i = 0; i < line.Length; i++)
    {
      var ch = line[i];

      if (ch == textQualifier)
      {
        if (insideQuotes && i + 1 < line.Length && line[i + 1] == textQualifier)
        {
          // Escaped quote
          current.Append(textQualifier);
          i++; // Skip next quote
        }
        else
        {
          insideQuotes = !insideQuotes;
        }
      }
      else if (ch == delimiter && !insideQuotes)
      {
        result.Add(current.ToString());
        current.Clear();
      }
      else
      {
        current.Append(ch);
      }
    }

    result.Add(current.ToString());
    return result.ToArray();
  }

  /// <summary>
  /// Escapa valor para CSV
  /// </summary>
  private string EscapeCsvValue(string value, char delimiter, char textQualifier)
  {
    if (string.IsNullOrEmpty(value))
      return "";

    var needsEscaping = value.Contains(delimiter) ||
                       value.Contains(textQualifier) ||
                       value.Contains('\n') ||
                       value.Contains('\r');

    if (!needsEscaping)
      return value;

    // Escape qualificadores de texto duplicando-os
    var escaped = value.Replace(textQualifier.ToString(), $"{textQualifier}{textQualifier}");

    return $"{textQualifier}{escaped}{textQualifier}";
  }

  /// <summary>
  /// Adiciona linha de dados ao DataTable
  /// </summary>
  private void AddDataRow(DataTable dataTable, string[] values)
  {
    var row = dataTable.NewRow();

    for (int i = 0; i < Math.Min(values.Length, dataTable.Columns.Count); i++)
    {
      row[i] = values[i];
    }

    dataTable.Rows.Add(row);
  }
}

/// <summary>
/// Opções para importação/exportação de CSV
/// </summary>
public class CsvOptions
{
  public char Delimiter { get; set; } = '\0'; // Auto-detect if \0
  public char TextQualifier { get; set; } = '"';
  public bool HasHeaders { get; set; } = true;
  public bool SkipEmptyLines { get; set; } = true;
  public Encoding Encoding { get; set; } = Encoding.UTF8;
}