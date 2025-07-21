using System.Data;
using IronXL;

namespace RealQuery.Core.Services;

/// <summary>
/// Serviço para manipular arquivos Excel usando IronXL
/// </summary>
public class ExcelDataService : IDataService
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
  /// Importa dados de um arquivo Excel
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
        // Carregar workbook
        var workbook = WorkBook.Load(filePath);

        // Pegar primeira worksheet por padrão
        var worksheet = workbook.DefaultWorkSheet ?? workbook.WorkSheets.FirstOrDefault();

        if (worksheet == null)
          throw new InvalidOperationException("Nenhuma planilha encontrada no arquivo Excel.");

        // Converter para DataTable
        return ConvertWorksheetToDataTable(worksheet);
      }
      catch (Exception ex)
      {
        throw new InvalidOperationException($"Erro ao importar Excel: {ex.Message}", ex);
      }
    });
  }

  /// <summary>
  /// Exporta dados para um arquivo Excel
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
        // Criar novo workbook
        var workbook = WorkBook.Create();
        var worksheet = workbook.DefaultWorkSheet;
        worksheet.Name = "Data";

        // Escrever headers
        for (int col = 0; col < data.Columns.Count; col++)
        {
          worksheet[0, col].Value = data.Columns[col].ColumnName;
          worksheet[0, col].Style.Font.Bold = true;
        }

        // Escrever dados
        for (int row = 0; row < data.Rows.Count; row++)
        {
          for (int col = 0; col < data.Columns.Count; col++)
          {
            var value = data.Rows[row][col];
            worksheet[row + 1, col].Value = value?.ToString() ?? "";
          }
        }

        // Auto-fit columns
        foreach (var column in worksheet.Columns)
        {
          column.AutoSizeColumn();
        }

        // Salvar arquivo
        workbook.SaveAs(filePath);
      }
      catch (Exception ex)
      {
        throw new InvalidOperationException($"Erro ao exportar Excel: {ex.Message}", ex);
      }
    });
  }

  /// <summary>
  /// Importa dados de uma worksheet específica
  /// </summary>
  public async Task<DataTable> ImportFromWorksheetAsync(string filePath, string worksheetName)
  {
    if (!File.Exists(filePath))
      throw new FileNotFoundException($"Arquivo não encontrado: {filePath}");

    return await Task.Run(() =>
    {
      try
      {
        var workbook = WorkBook.Load(filePath);
        var worksheet = workbook.WorkSheets[worksheetName];

        if (worksheet == null)
          throw new InvalidOperationException($"Planilha '{worksheetName}' não encontrada.");

        return ConvertWorksheetToDataTable(worksheet);
      }
      catch (Exception ex)
      {
        throw new InvalidOperationException($"Erro ao importar planilha: {ex.Message}", ex);
      }
    });
  }

  /// <summary>
  /// Obtém lista de worksheets disponíveis no arquivo
  /// </summary>
  public async Task<string[]> GetWorksheetsAsync(string filePath)
  {
    if (!File.Exists(filePath))
      throw new FileNotFoundException($"Arquivo não encontrado: {filePath}");

    return await Task.Run(() =>
    {
      try
      {
        var workbook = WorkBook.Load(filePath);
        return workbook.WorkSheets.Select(ws => ws.Name).ToArray();
      }
      catch (Exception ex)
      {
        throw new InvalidOperationException($"Erro ao listar planilhas: {ex.Message}", ex);
      }
    });
  }

  /// <summary>
  /// Converte uma WorkSheet do IronXL para DataTable
  /// </summary>
  private DataTable ConvertWorksheetToDataTable(WorkSheet worksheet)
  {
    var dataTable = new DataTable();

    if (worksheet.Dimension == null || worksheet.Dimension.Rows == 0)
      return dataTable;

    // Detectar range de dados
    var firstRow = worksheet.Dimension.TopLeftCellAddress.Row;
    var lastRow = worksheet.Dimension.BottomRightCellAddress.Row;
    var firstColumn = worksheet.Dimension.TopLeftCellAddress.Column;
    var lastColumn = worksheet.Dimension.BottomRightCellAddress.Column;

    // Criar colunas baseadas na primeira linha (headers)
    for (int col = firstColumn; col <= lastColumn; col++)
    {
      var headerCell = worksheet[firstRow, col];
      var columnName = headerCell?.Value?.ToString() ?? $"Column{col}";

      // Garantir nomes únicos de colunas
      var originalName = columnName;
      var counter = 1;
      while (dataTable.Columns.Contains(columnName))
      {
        columnName = $"{originalName}_{counter++}";
      }

      dataTable.Columns.Add(columnName);
    }

    // Adicionar dados (começando da segunda linha)
    for (int row = firstRow + 1; row <= lastRow; row++)
    {
      var dataRow = dataTable.NewRow();
      var hasData = false;

      for (int col = firstColumn; col <= lastColumn; col++)
      {
        var cell = worksheet[row, col];
        var value = cell?.Value?.ToString() ?? "";

        if (!string.IsNullOrEmpty(value))
          hasData = true;

        dataRow[col - firstColumn] = value;
      }

      // Só adicionar linha se tiver pelo menos um valor
      if (hasData)
        dataTable.Rows.Add(dataRow);
    }

    return dataTable;
  }

  /// <summary>
  /// Obtém informações sobre o arquivo Excel
  /// </summary>
  public async Task<ExcelFileInfo> GetFileInfoAsync(string filePath)
  {
    if (!File.Exists(filePath))
      throw new FileNotFoundException($"Arquivo não encontrado: {filePath}");

    return await Task.Run(() =>
    {
      try
      {
        var workbook = WorkBook.Load(filePath);
        var worksheets = workbook.WorkSheets.Select(ws => new WorksheetInfo
        {
          Name = ws.Name,
          RowCount = ws.Dimension?.Rows ?? 0,
          ColumnCount = ws.Dimension?.Columns ?? 0
        }).ToArray();

        return new ExcelFileInfo
        {
          FilePath = filePath,
          FileName = Path.GetFileName(filePath),
          FileSize = new FileInfo(filePath).Length,
          WorksheetCount = worksheets.Length,
          Worksheets = worksheets
        };
      }
      catch (Exception ex)
      {
        throw new InvalidOperationException($"Erro ao obter informações do arquivo: {ex.Message}", ex);
      }
    });
  }
}

/// <summary>
/// Informações sobre um arquivo Excel
/// </summary>
public class ExcelFileInfo
{
  public string FilePath { get; set; } = "";
  public string FileName { get; set; } = "";
  public long FileSize { get; set; }
  public int WorksheetCount { get; set; }
  public WorksheetInfo[] Worksheets { get; set; } = Array.Empty<WorksheetInfo>();
}

/// <summary>
/// Informações sobre uma worksheet
/// </summary>
public class WorksheetInfo
{
  public string Name { get; set; } = "";
  public int RowCount { get; set; }
  public int ColumnCount { get; set; }
}