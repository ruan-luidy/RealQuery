using System.Data;
using RealQuery.Core.Models;
using PipeFlow.Core;
using PipeFlow.Core.Excel;
using ClosedXML.Excel;

namespace RealQuery.Core.Services;

/// <summary>
/// Serviço para manipular arquivos Excel usando PipeFlowCore
/// </summary>
public class PipeFlowExcelDataService : IDataService
{
  public string[] SupportedExtensions => new[] { ".xlsx", ".xls", ".xlsm" };

  public bool CanHandle(string filePath)
  {
    if (string.IsNullOrEmpty(filePath))
      return false;

    var extension = Path.GetExtension(filePath).ToLowerInvariant();
    return SupportedExtensions.Contains(extension);
  }

  /// <summary>
  /// Importa dados de um arquivo Excel usando PipeFlow
  /// </summary>
  public async Task<DataTable> ImportAsync(string filePath)
  {
    if (!File.Exists(filePath))
      throw new FileNotFoundException($"Arquivo não encontrado: {filePath}");

    if (!CanHandle(filePath))
      throw new NotSupportedException($"Formato de arquivo não suportado: {Path.GetExtension(filePath)}");

    await Task.CompletedTask; // Manter async para consistência com interface

    // Usar PipeFlow para ler Excel
    var pipeline = PipeFlow.Core.PipeFlow.From.Excel(filePath);
    var pipeFlowRows = pipeline.Execute();

    // Converter para DataTable
    return PipeFlowDataBridge.ToDataTable(pipeFlowRows);
  }

  /// <summary>
  /// Exporta dados para um arquivo Excel usando PipeFlow
  /// </summary>
  public async Task ExportAsync(DataTable data, string filePath)
  {
    if (data == null)
      throw new ArgumentNullException(nameof(data));

    if (string.IsNullOrEmpty(filePath))
      throw new ArgumentNullException(nameof(filePath));

    if (!CanHandle(filePath))
      throw new NotSupportedException($"Formato de arquivo não suportado: {Path.GetExtension(filePath)}");

    await Task.CompletedTask; // Manter async para consistência

    // Converter DataTable para PipeFlow.Core.DataRow
    var pipeFlowRows = PipeFlowDataBridge.ToPipeFlowRows(data);

    // Criar pipeline e exportar para Excel
    var pipeline = PipeFlow.Core.PipeFlow.From.DataRows(pipeFlowRows);
    pipeline.ToExcel(filePath, writer => {
      writer.WithHeaders(true);
      writer.WithAutoFit(true);
    });
  }

  /// <summary>
  /// Importa dados de uma worksheet específica
  /// </summary>
  public async Task<DataTable> ImportFromWorksheetAsync(string filePath, string worksheetName)
  {
    if (!File.Exists(filePath))
      throw new FileNotFoundException($"Arquivo não encontrado: {filePath}");

    if (string.IsNullOrEmpty(worksheetName))
      throw new ArgumentNullException(nameof(worksheetName));

    await Task.CompletedTask;

    // Usar PipeFlow com configuração de worksheet específica
    var pipeline = PipeFlow.Core.PipeFlow.From.Excel(filePath, reader => {
      reader.Sheet(worksheetName);
      reader.WithHeaders(true);
    });

    var pipeFlowRows = pipeline.Execute();
    return PipeFlowDataBridge.ToDataTable(pipeFlowRows);
  }

  /// <summary>
  /// Obtém lista de worksheets disponíveis no arquivo
  /// </summary>
  public async Task<string[]> GetWorksheetsAsync(string filePath)
  {
    if (!File.Exists(filePath))
      throw new FileNotFoundException($"Arquivo não encontrado: {filePath}");

    await Task.CompletedTask;

    // Usar ClosedXML diretamente para obter informações do arquivo
    using var workbook = new XLWorkbook(filePath);
    return workbook.Worksheets.Select(ws => ws.Name).ToArray();
  }

  /// <summary>
  /// Obtém informações sobre o arquivo Excel
  /// </summary>
  public async Task<ExcelFileInfo> GetFileInfoAsync(string filePath)
  {
    if (!File.Exists(filePath))
      throw new FileNotFoundException($"Arquivo não encontrado: {filePath}");

    await Task.CompletedTask;

    var fileInfo = new FileInfo(filePath);
    var worksheets = new List<WorksheetInfo>();

    using var workbook = new XLWorkbook(filePath);

    foreach (var worksheet in workbook.Worksheets)
    {
      var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;
      var lastColumn = worksheet.LastColumnUsed()?.ColumnNumber() ?? 0;

      worksheets.Add(new WorksheetInfo
      {
        Name = worksheet.Name,
        RowCount = lastRow,
        ColumnCount = lastColumn
      });
    }

    return new ExcelFileInfo
    {
      FilePath = filePath,
      FileName = fileInfo.Name,
      FileSize = fileInfo.Length,
      WorksheetCount = workbook.Worksheets.Count,
      Worksheets = worksheets.ToArray()
    };
  }
}
