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

        // Converter para DataTable usando método nativo do IronXL
        return worksheet.ToDataTable(true); // true = primeira linha como headers
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
          var cell = worksheet[$"{GetColumnName(col)}1"];
          cell.Value = data.Columns[col].ColumnName;
          cell.Style.Font.Bold = true;
        }

        // Escrever dados
        for (int row = 0; row < data.Rows.Count; row++)
        {
          for (int col = 0; col < data.Columns.Count; col++)
          {
            var cell = worksheet[$"{GetColumnName(col)}{row + 2}"];
            var value = data.Rows[row][col];
            cell.Value = value?.ToString() ?? "";
          }
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
        var worksheet = workbook.WorkSheets.FirstOrDefault(ws => ws.Name == worksheetName);

        if (worksheet == null)
          throw new InvalidOperationException($"Planilha '{worksheetName}' não encontrada.");

        return worksheet.ToDataTable(true);
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
  /// Converte índice numérico de coluna para letra (0 = A, 1 = B, etc.)
  /// </summary>
  private string GetColumnName(int columnIndex)
  {
    var columnName = "";
    while (columnIndex >= 0)
    {
      columnName = (char)('A' + columnIndex % 26) + columnName;
      columnIndex = columnIndex / 26 - 1;
    }
    return columnName;
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
        var worksheets = workbook.WorkSheets.Select(ws =>
        {
          // Método simples para contar linhas e colunas usando ToDataTable
          int rowCount = 0;
          int colCount = 0;

          try
          {
            var tempTable = ws.ToDataTable(true);
            rowCount = tempTable.Rows.Count;
            colCount = tempTable.Columns.Count;
          }
          catch
          {
            // Se falhar, usar valores padrão
            rowCount = 0;
            colCount = 0;
          }

          return new WorksheetInfo
          {
            Name = ws.Name,
            RowCount = rowCount,
            ColumnCount = colCount
          };
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