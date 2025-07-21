using System.Data;
using System.Diagnostics;
using System.Windows;
using RealQuery.Core.Models;
using RealQuery.Core.Services;

namespace RealQuery.ViewModels;

/// <summary>
/// ViewModel principal da aplicação
/// </summary>
public partial class MainViewModel : ObservableObject
{
  #region Services
  private readonly ExcelDataService _excelService;
  private readonly CsvDataService _csvService;
  private readonly FileDialogService _fileDialogService;
  #endregion

  #region Observable Properties

  [ObservableProperty]
  private DataTable? _currentData;

  [ObservableProperty]
  private string _cSharpCode = "// Write your C# transformation code here\n// Example:\n// data = data.Where(row => row[\"Age\"] > 18);";

  [ObservableProperty]
  private string _statusMessage = "Ready";

  [ObservableProperty]
  private int _rowCount;

  [ObservableProperty]
  private int _columnCount;

  [ObservableProperty]
  private string _lastExecutionTime = "-";

  [ObservableProperty]
  private bool _isProcessing;

  [ObservableProperty]
  private ObservableCollection<TransformationStep> _transformationSteps = new();

  [ObservableProperty]
  private TransformationStep? _selectedStep;

  #endregion

  #region Commands

  [RelayCommand]
  private async Task ImportDataAsync()
  {
    try
    {
      IsProcessing = true;
      StatusMessage = "Selecting file...";

      var filePath = _fileDialogService.OpenFileDialog(
          "Import Data",
          _fileDialogService.GetDefaultImportFilter()
      );

      if (string.IsNullOrEmpty(filePath))
      {
        StatusMessage = "Import canceled";
        return;
      }

      StatusMessage = "Importing data...";

      // Validar arquivo
      if (!_fileDialogService.ValidateFile(filePath, out string errorMessage))
      {
        ShowError($"File validation error:\n{errorMessage}");
        StatusMessage = "Import failed";
        return;
      }

      // Determinar serviço baseado na extensão
      IDataService dataService = GetDataServiceForFile(filePath);

      var stopwatch = Stopwatch.StartNew();
      CurrentData = await dataService.ImportAsync(filePath);
      stopwatch.Stop();

      // Atualizar estatísticas
      UpdateDataInfo(CurrentData);

      // Adicionar step
      var step = TransformationStep.CreateImportStep(
          Path.GetFileName(filePath),
          CurrentData.Rows.Count
      );
      step.StepNumber = TransformationSteps.Count + 1;
      step.ExecutionTime = stopwatch.Elapsed;

      TransformationSteps.Add(step);

      StatusMessage = $"Import completed: {CurrentData.Rows.Count:N0} rows loaded";
    }
    catch (Exception ex)
    {
      ShowError($"Import error:\n{ex.Message}");
      StatusMessage = "Import failed";
    }
    finally
    {
      IsProcessing = false;
    }
  }

  [RelayCommand]
  private async Task ExportDataAsync()
  {
    if (CurrentData == null || CurrentData.Rows.Count == 0)
    {
      ShowWarning("No data to export.\nPlease import or generate data first.");
      return;
    }

    try
    {
      IsProcessing = true;
      StatusMessage = "Selecting export location...";

      var defaultFileName = $"RealQuery_Export_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
      var filePath = _fileDialogService.SaveFileDialog(
          "Export Data",
          defaultFileName,
          _fileDialogService.GetDefaultExportFilter()
      );

      if (string.IsNullOrEmpty(filePath))
      {
        StatusMessage = "Export canceled";
        return;
      }

      StatusMessage = "Exporting data...";

      // Validar caminho de saída
      if (!_fileDialogService.ValidateOutputPath(filePath, out string errorMessage))
      {
        ShowError($"Output path validation error:\n{errorMessage}");
        StatusMessage = "Export failed";
        return;
      }

      // Determinar serviço baseado na extensão
      IDataService dataService = GetDataServiceForFile(filePath);

      var stopwatch = Stopwatch.StartNew();
      await dataService.ExportAsync(CurrentData, filePath);
      stopwatch.Stop();

      // Adicionar step
      var step = TransformationStep.CreateExportStep(
          Path.GetFileName(filePath),
          CurrentData.Rows.Count
      );
      step.StepNumber = TransformationSteps.Count + 1;
      step.ExecutionTime = stopwatch.Elapsed;

      TransformationSteps.Add(step);

      StatusMessage = $"Export completed: {Path.GetFileName(filePath)}";

      // Perguntar se quer abrir o arquivo
      var result = MessageBox.Show(
          $"File exported successfully!\n\n{filePath}\n\nWould you like to open the file?",
          "Export Completed",
          MessageBoxButton.YesNo,
          MessageBoxImage.Information
      );

      if (result == MessageBoxResult.Yes)
      {
        Process.Start(new ProcessStartInfo
        {
          FileName = filePath,
          UseShellExecute = true
        });
      }
    }
    catch (Exception ex)
    {
      ShowError($"Export error:\n{ex.Message}");
      StatusMessage = "Export failed";
    }
    finally
    {
      IsProcessing = false;
    }
  }

  [RelayCommand]
  private async Task ExecuteCodeAsync()
  {
    if (CurrentData == null)
    {
      ShowWarning("No data available.\nPlease import data first.");
      return;
    }

    if (string.IsNullOrWhiteSpace(CSharpCode))
    {
      ShowWarning("No code to execute.\nPlease write some C# transformation code.");
      return;
    }

    try
    {
      IsProcessing = true;
      StatusMessage = "Executing C# code...";

      var inputRows = CurrentData.Rows.Count;
      var step = TransformationStep.CreateCodeStep(CSharpCode, inputRows);
      step.StepNumber = TransformationSteps.Count + 1;

      var stopwatch = Stopwatch.StartNew();

      // TODO: Implementar execução real com Roslyn
      // Por enquanto, simular transformação
      await Task.Delay(500); // Simular processamento

      stopwatch.Stop();

      // Simular resultado (remover quando Roslyn estiver implementado)
      var outputRows = CurrentData.Rows.Count;
      step.MarkAsExecuted(stopwatch.Elapsed, outputRows);

      TransformationSteps.Add(step);

      LastExecutionTime = $"{stopwatch.Elapsed.TotalSeconds:F2}s";
      StatusMessage = "Code execution completed";

      ShowInfo("Code execution completed!\n\nNote: Roslyn C# execution will be implemented in the next phase.");
    }
    catch (Exception ex)
    {
      var step = TransformationStep.CreateCodeStep(CSharpCode);
      step.StepNumber = TransformationSteps.Count + 1;
      step.MarkAsError(ex.Message);
      TransformationSteps.Add(step);

      ShowError($"Code execution error:\n{ex.Message}");
      StatusMessage = "Code execution failed";
    }
    finally
    {
      IsProcessing = false;
    }
  }

  [RelayCommand]
  private void ClearAll()
  {
    var result = MessageBox.Show(
        "Are you sure you want to clear all data and transformations?",
        "Confirm Clear",
        MessageBoxButton.YesNo,
        MessageBoxImage.Question
    );

    if (result == MessageBoxResult.Yes)
    {
      CurrentData = null;
      TransformationSteps.Clear();
      CSharpCode = "// Write your C# transformation code here\n// Example:\n// data = data.Where(row => row[\"Age\"] > 18);";

      RowCount = 0;
      ColumnCount = 0;
      LastExecutionTime = "-";
      StatusMessage = "Data cleared";
    }
  }

  [RelayCommand]
  private void SaveWorkspace()
  {
    // TODO: Implementar salvamento de workspace
    ShowInfo("Workspace save functionality will be implemented soon!\n\nYou'll be able to save and load complete workspaces with all transformations.");
    StatusMessage = "Workspace save - coming soon...";
  }

  [RelayCommand]
  private void RemoveStep(TransformationStep step)
  {
    if (step != null && TransformationSteps.Contains(step))
    {
      var result = MessageBox.Show(
          $"Remove step '{step.Title}'?",
          "Confirm Remove",
          MessageBoxButton.YesNo,
          MessageBoxImage.Question
      );

      if (result == MessageBoxResult.Yes)
      {
        TransformationSteps.Remove(step);
        RenumberSteps();
        StatusMessage = $"Removed step: {step.Title}";
      }
    }
  }

  [RelayCommand]
  private void ClearSteps()
  {
    if (TransformationSteps.Count == 0) return;

    var result = MessageBox.Show(
        "Clear all transformation steps?",
        "Confirm Clear Steps",
        MessageBoxButton.YesNo,
        MessageBoxImage.Question
    );

    if (result == MessageBoxResult.Yes)
    {
      TransformationSteps.Clear();
      StatusMessage = "All steps cleared";
    }
  }

  #endregion

  #region Constructor

  public MainViewModel()
  {
    _excelService = new ExcelDataService();
    _csvService = new CsvDataService();
    _fileDialogService = new FileDialogService();

    // Carregar dados de exemplo
    LoadSampleData();
  }

  #endregion

  #region Private Methods

  /// <summary>
  /// Determina qual serviço usar baseado na extensão do arquivo
  /// </summary>
  private IDataService GetDataServiceForFile(string filePath)
  {
    if (_excelService.CanHandle(filePath))
      return _excelService;

    if (_csvService.CanHandle(filePath))
      return _csvService;

    throw new NotSupportedException($"File format not supported: {Path.GetExtension(filePath)}");
  }

  /// <summary>
  /// Atualiza informações dos dados
  /// </summary>
  private void UpdateDataInfo(DataTable data)
  {
    RowCount = data.Rows.Count;
    ColumnCount = data.Columns.Count;
  }

  /// <summary>
  /// Renumera os steps após remoção
  /// </summary>
  private void RenumberSteps()
  {
    for (int i = 0; i < TransformationSteps.Count; i++)
    {
      TransformationSteps[i].StepNumber = i + 1;
    }
  }

  /// <summary>
  /// Carrega dados de exemplo para demonstração
  /// </summary>
  private void LoadSampleData()
  {
    var sampleData = new DataTable();
    sampleData.Columns.Add("ID", typeof(int));
    sampleData.Columns.Add("Name", typeof(string));
    sampleData.Columns.Add("Age", typeof(int));
    sampleData.Columns.Add("City", typeof(string));
    sampleData.Columns.Add("Salary", typeof(decimal));

    sampleData.Rows.Add(1, "João Silva", 28, "São Paulo", 5500.00m);
    sampleData.Rows.Add(2, "Maria Santos", 32, "Rio de Janeiro", 6200.00m);
    sampleData.Rows.Add(3, "Carlos Oliveira", 25, "Belo Horizonte", 4800.00m);
    sampleData.Rows.Add(4, "Ana Costa", 35, "Porto Alegre", 7100.00m);
    sampleData.Rows.Add(5, "Pedro Lima", 29, "Recife", 5300.00m);
    sampleData.Rows.Add(6, "Lucia Ferreira", 31, "Salvador", 5900.00m);
    sampleData.Rows.Add(7, "Roberto Alves", 27, "Fortaleza", 5100.00m);
    sampleData.Rows.Add(8, "Fernanda Rocha", 33, "Brasília", 6800.00m);

    CurrentData = sampleData;
    UpdateDataInfo(sampleData);

    var sampleStep = TransformationStep.CreateImportStep("Sample Data", sampleData.Rows.Count);
    sampleStep.StepNumber = 1;
    TransformationSteps.Add(sampleStep);
  }

  /// <summary>
  /// Mostra mensagem de erro
  /// </summary>
  private void ShowError(string message)
  {
    MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
  }

  /// <summary>
  /// Mostra mensagem de aviso
  /// </summary>
  private void ShowWarning(string message)
  {
    MessageBox.Show(message, "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
  }

  /// <summary>
  /// Mostra mensagem informativa
  /// </summary>
  private void ShowInfo(string message)
  {
    MessageBox.Show(message, "Information", MessageBoxButton.OK, MessageBoxImage.Information);
  }

  #endregion
}