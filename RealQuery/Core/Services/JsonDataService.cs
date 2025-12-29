using System.Data;
using PipeFlow.Core;

namespace RealQuery.Core.Services;

/// <summary>
/// Serviço para manipular arquivos JSON usando PipeFlowCore
/// </summary>
public class JsonDataService : IDataService
{
  public string[] SupportedExtensions => new[] { ".json", ".jsonl" };

  public bool CanHandle(string filePath)
  {
    if (string.IsNullOrEmpty(filePath))
      return false;

    var extension = Path.GetExtension(filePath).ToLowerInvariant();
    return SupportedExtensions.Contains(extension);
  }

  /// <summary>
  /// Importa dados de um arquivo JSON usando PipeFlow
  /// </summary>
  public async Task<DataTable> ImportAsync(string filePath)
  {
    if (!File.Exists(filePath))
      throw new FileNotFoundException($"Arquivo não encontrado: {filePath}");

    if (!CanHandle(filePath))
      throw new NotSupportedException($"Formato de arquivo não suportado: {Path.GetExtension(filePath)}");

    await Task.CompletedTask;

    // Usar PipeFlow para ler JSON (suporta .json e .jsonl)
    var pipeline = PipeFlow.Core.PipeFlow.From.Json(filePath);
    var pipeFlowRows = pipeline.Execute();

    return PipeFlowDataBridge.ToDataTable(pipeFlowRows);
  }

  /// <summary>
  /// Exporta dados para um arquivo JSON usando PipeFlow
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

    // Criar pipeline e exportar para JSON (pretty-printed por padrão)
    var pipeline = PipeFlow.Core.PipeFlow.From.DataRows(pipeFlowRows);
    pipeline.ToJson(filePath, writer => {
      writer.WithIndentation(true); // Pretty print
    });
  }
}
