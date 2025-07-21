using System.Windows;
using WinForms = System.Windows.Forms;

namespace RealQuery.Core.Services;

/// <summary>
/// Serviço para manipular diálogos de arquivos
/// </summary>
public class FileDialogService
{
  /// <summary>
  /// Abre diálogo para selecionar arquivo de importação
  /// </summary>
  public string? OpenFileDialog(string title = "Importar Dados", string filter = "")
  {
    var dialog = new WinForms.OpenFileDialog
    {
      Title = title,
      Filter = string.IsNullOrEmpty(filter) ? GetDefaultImportFilter() : filter,
      FilterIndex = 1,
      CheckFileExists = true,
      CheckPathExists = true,
      Multiselect = false
    };

    return dialog.ShowDialog() == WinForms.DialogResult.OK ? dialog.FileName : null;
  }

  /// <summary>
  /// Abre diálogo para selecionar múltiplos arquivos
  /// </summary>
  public string[]? OpenMultipleFilesDialog(string title = "Importar Múltiplos Arquivos", string filter = "")
  {
    var dialog = new WinForms.OpenFileDialog
    {
      Title = title,
      Filter = string.IsNullOrEmpty(filter) ? GetDefaultImportFilter() : filter,
      FilterIndex = 1,
      CheckFileExists = true,
      CheckPathExists = true,
      Multiselect = true
    };

    return dialog.ShowDialog() == WinForms.DialogResult.OK ? dialog.FileNames : null;
  }

  /// <summary>
  /// Abre diálogo para salvar arquivo
  /// </summary>
  public string? SaveFileDialog(string title = "Exportar Dados", string defaultFileName = "", string filter = "")
  {
    var dialog = new WinForms.SaveFileDialog
    {
      Title = title,
      Filter = string.IsNullOrEmpty(filter) ? GetDefaultExportFilter() : filter,
      FilterIndex = 1,
      CheckPathExists = true,
      AddExtension = true,
      FileName = defaultFileName
    };

    return dialog.ShowDialog() == WinForms.DialogResult.OK ? dialog.FileName : null;
  }

  /// <summary>
  /// Abre diálogo para selecionar pasta
  /// </summary>
  public string? OpenFolderDialog(string title = "Selecionar Pasta")
  {
    var dialog = new WinForms.FolderBrowserDialog
    {
      Description = title,
      UseDescriptionForTitle = true,
      ShowNewFolderButton = true
    };

    return dialog.ShowDialog() == WinForms.DialogResult.OK ? dialog.SelectedPath : null;
  }

  /// <summary>
  /// Obtém filtro padrão para importação
  /// </summary>
  private string GetDefaultImportFilter()
  {
    return "Todos os formatos suportados|*.xlsx;*.xls;*.xlsm;*.csv;*.txt|" +
           "Arquivos Excel|*.xlsx;*.xls;*.xlsm|" +
           "Arquivos CSV|*.csv|" +
           "Arquivos de Texto|*.txt|" +
           "Todos os arquivos|*.*";
  }

  /// <summary>
  /// Obtém filtro padrão para exportação
  /// </summary>
  private string GetDefaultExportFilter()
  {
    return "Excel Workbook|*.xlsx|" +
           "Excel 97-2003|*.xls|" +
           "CSV (separado por vírgula)|*.csv|" +
           "Arquivo de Texto|*.txt|" +
           "Todos os arquivos|*.*";
  }

  /// <summary>
  /// Obtém filtro específico para Excel
  /// </summary>
  public string GetExcelFilter()
  {
    return "Arquivos Excel|*.xlsx;*.xls;*.xlsm|" +
           "Excel Workbook (*.xlsx)|*.xlsx|" +
           "Excel 97-2003 (*.xls)|*.xls|" +
           "Excel Macro-Enabled (*.xlsm)|*.xlsm";
  }

  /// <summary>
  /// Obtém filtro específico para CSV
  /// </summary>
  public string GetCsvFilter()
  {
    return "Arquivos CSV|*.csv|" +
           "Arquivos de Texto|*.txt|" +
           "Todos os arquivos|*.*";
  }

  /// <summary>
  /// Valida se o arquivo existe e pode ser lido
  /// </summary>
  public bool ValidateFile(string filePath, out string errorMessage)
  {
    errorMessage = "";

    if (string.IsNullOrEmpty(filePath))
    {
      errorMessage = "Caminho do arquivo não pode estar vazio.";
      return false;
    }

    if (!File.Exists(filePath))
    {
      errorMessage = "Arquivo não encontrado.";
      return false;
    }

    try
    {
      // Testar se pode abrir o arquivo para leitura
      using var fs = File.OpenRead(filePath);
      return true;
    }
    catch (UnauthorizedAccessException)
    {
      errorMessage = "Sem permissão para acessar o arquivo.";
      return false;
    }
    catch (IOException)
    {
      errorMessage = "Arquivo está sendo usado por outro processo.";
      return false;
    }
    catch (Exception ex)
    {
      errorMessage = $"Erro ao acessar arquivo: {ex.Message}";
      return false;
    }
  }

  /// <summary>
  /// Valida se o diretório de destino existe e pode ser escrito
  /// </summary>
  public bool ValidateOutputPath(string filePath, out string errorMessage)
  {
    errorMessage = "";

    if (string.IsNullOrEmpty(filePath))
    {
      errorMessage = "Caminho do arquivo não pode estar vazio.";
      return false;
    }

    var directory = Path.GetDirectoryName(filePath);
    if (string.IsNullOrEmpty(directory))
    {
      errorMessage = "Diretório não especificado.";
      return false;
    }

    if (!Directory.Exists(directory))
    {
      try
      {
        Directory.CreateDirectory(directory);
      }
      catch (Exception ex)
      {
        errorMessage = $"Erro ao criar diretório: {ex.Message}";
        return false;
      }
    }

    try
    {
      // Testar se pode escrever no diretório
      var testFile = Path.Combine(directory, $"test_{Guid.NewGuid()}.tmp");
      File.WriteAllText(testFile, "test");
      File.Delete(testFile);
      return true;
    }
    catch (UnauthorizedAccessException)
    {
      errorMessage = "Sem permissão para escrever no diretório.";
      return false;
    }
    catch (Exception ex)
    {
      errorMessage = $"Erro ao testar escrita: {ex.Message}";
      return false;
    }
  }

  /// <summary>
  /// Obtém informações sobre um arquivo
  /// </summary>
  public FileInfo GetFileInfo(string filePath)
  {
    if (!File.Exists(filePath))
      throw new FileNotFoundException($"Arquivo não encontrado: {filePath}");

    return new FileInfo(filePath);
  }

  /// <summary>
  /// Formata tamanho de arquivo para exibição
  /// </summary>
  public string FormatFileSize(long bytes)
  {
    string[] sizes = { "B", "KB", "MB", "GB", "TB" };
    double len = bytes;
    int order = 0;

    while (len >= 1024 && order < sizes.Length - 1)
    {
      order++;
      len = len / 1024;
    }

    return $"{len:0.##} {sizes[order]}";
  }
}