namespace RealQuery.Core.Models;

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
