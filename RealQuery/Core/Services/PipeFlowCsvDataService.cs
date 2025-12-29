using System.Data;
using System.Text;
using RealQuery.Core.Models;
using PipeFlow.Core;

namespace RealQuery.Core.Services;

/// <summary>
/// Serviço para manipular arquivos CSV usando PipeFlowCore
/// </summary>
public class PipeFlowCsvDataService : IDataService
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
  /// Importa dados de um arquivo CSV usando PipeFlow
  /// </summary>
  public async Task<DataTable> ImportAsync(string filePath)
  {
    if (!File.Exists(filePath))
      throw new FileNotFoundException($"Arquivo não encontrado: {filePath}");

    if (!CanHandle(filePath))
      throw new NotSupportedException($"Formato de arquivo não suportado: {Path.GetExtension(filePath)}");

    await Task.CompletedTask;

    // Auto-detectar delimitador
    var delimiter = DetectDelimiterSync(filePath);

    // Usar PipeFlow para ler CSV
    var pipeline = PipeFlow.Core.PipeFlow.From.Csv(filePath, reader => {
      reader.WithDelimiter(delimiter.ToString());
      reader.WithHeaders(true);
      reader.WithEncoding(Encoding.UTF8);
      reader.WithTrimming(true);
    });

    var pipeFlowRows = pipeline.Execute();
    return PipeFlowDataBridge.ToDataTable(pipeFlowRows);
  }

  /// <summary>
  /// Exporta dados para um arquivo CSV usando PipeFlow
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

    // Converter DataTable para PipeFlow.Core.DataRow
    var pipeFlowRows = PipeFlowDataBridge.ToPipeFlowRows(data);

    // Criar pipeline e exportar para CSV
    var pipeline = PipeFlow.Core.PipeFlow.From.DataRows(pipeFlowRows);
    pipeline.ToCsv(filePath, writer => {
      writer.WithDelimiter(",");
      writer.WithHeaders(true);
      writer.WithEncoding(Encoding.UTF8);
    });
  }

  /// <summary>
  /// Importa CSV com opções customizadas
  /// </summary>
  public async Task<DataTable> ImportAsync(string filePath, CsvOptions options)
  {
    if (!File.Exists(filePath))
      throw new FileNotFoundException($"Arquivo não encontrado: {filePath}");

    if (options == null)
      throw new ArgumentNullException(nameof(options));

    await Task.CompletedTask;

    // Se o delimitador é \0, auto-detectar
    var delimiter = options.Delimiter == '\0' ? DetectDelimiterSync(filePath) : options.Delimiter;

    // Usar PipeFlow com opções customizadas
    var pipeline = PipeFlow.Core.PipeFlow.From.Csv(filePath, reader => {
      reader.WithDelimiter(delimiter.ToString());
      reader.WithHeaders(options.HasHeaders);
      reader.WithEncoding(options.Encoding);
      reader.WithTrimming(options.SkipEmptyLines);
    });

    var pipeFlowRows = pipeline.Execute();
    return PipeFlowDataBridge.ToDataTable(pipeFlowRows);
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

    if (options == null)
      throw new ArgumentNullException(nameof(options));

    await Task.CompletedTask;

    // Converter DataTable para PipeFlow.Core.DataRow
    var pipeFlowRows = PipeFlowDataBridge.ToPipeFlowRows(data);

    // Determinar delimitador
    var delimiter = options.Delimiter == '\0' ? ',' : options.Delimiter;

    // Criar pipeline e exportar para CSV com opções
    var pipeline = PipeFlow.Core.PipeFlow.From.DataRows(pipeFlowRows);
    pipeline.ToCsv(filePath, writer => {
      writer.WithDelimiter(delimiter.ToString());
      writer.WithHeaders(options.HasHeaders);
      writer.WithEncoding(options.Encoding);
    });
  }

  /// <summary>
  /// Detecta automaticamente o delimitador do CSV (async)
  /// </summary>
  public async Task<char> DetectDelimiterAsync(string filePath)
  {
    if (!File.Exists(filePath))
      throw new FileNotFoundException($"Arquivo não encontrado: {filePath}");

    return await Task.Run(() => DetectDelimiterSync(filePath));
  }

  /// <summary>
  /// Detecta automaticamente o delimitador do CSV (sync)
  /// </summary>
  private char DetectDelimiterSync(string filePath)
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
  }
}
