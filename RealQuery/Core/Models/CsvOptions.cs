using System.Text;

namespace RealQuery.Core.Models;

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
