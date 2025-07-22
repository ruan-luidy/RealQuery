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
  private readonly CSharpTransformationEngine _transformationEngine;
  #endregion

  #region Observable Properties

  [ObservableProperty]
  private DataTable? _currentData;

  [ObservableProperty]
  private string _cSharpCode = @"// Write your C# transformation code here
// Available variable: data (DataTable)

// Examples:
// Filter rows: data = data.AsEnumerable().Where(row => row.Field<int>(""Age"") > 25).CopyToDataTable();
// Add column: data.Columns.Add(""Status"", typeof(string));
// Sort data: data.DefaultView.Sort = ""Name ASC""; data = data.DefaultView.ToTable();

";

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

  [ObservableProperty]
  private bool _hasCodeErrors;

  [ObservableProperty]
  private string _codeValidationMessage = "";

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

      if (!_fileDialogService.ValidateFile(filePath, out string errorMessage))
      {
        ShowError($"File validation error:\n{errorMessage}");
        StatusMessage = "Import failed";
        return;
      }

      IDataService dataService = GetDataServiceForFile(filePath);
      var stopwatch = Stopwatch.StartNew();
      CurrentData = await dataService.ImportAsync(filePath);
      stopwatch.Stop();

      UpdateDataInfo(CurrentData);

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

      if (!_fileDialogService.ValidateOutputPath(filePath, out string errorMessage))
      {
        ShowError($"Output path validation error:\n{errorMessage}");
        StatusMessage = "Export failed";
        return;
      }

      IDataService dataService = GetDataServiceForFile(filePath);
      var stopwatch = Stopwatch.StartNew();
      await dataService.ExportAsync(CurrentData, filePath);
      stopwatch.Stop();

      var step = TransformationStep.CreateExportStep(
          Path.GetFileName(filePath),
          CurrentData.Rows.Count
      );
      step.StepNumber = TransformationSteps.Count + 1;
      step.ExecutionTime = stopwatch.Elapsed;

      TransformationSteps.Add(step);
      StatusMessage = $"Export completed: {Path.GetFileName(filePath)}";

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

      // EXECUÇÃO REAL COM ROSLYN!
      var result = await _transformationEngine.ExecuteAsync(CSharpCode, CurrentData);

      if (result.Success && result.ResultData != null)
      {
        // Atualizar dados com resultado da transformação
        CurrentData = result.ResultData;
        UpdateDataInfo(CurrentData);

        step.MarkAsExecuted(result.ExecutionTime, CurrentData.Rows.Count);
        TransformationSteps.Add(step);

        LastExecutionTime = $"{result.ExecutionTime.TotalSeconds:F2}s";
        StatusMessage = result.Message;

        // Limpar erros
        HasCodeErrors = false;
        CodeValidationMessage = "";
      }
      else
      {
        // Erro na execução
        step.MarkAsError(result.ErrorMessage ?? "Unknown error");
        TransformationSteps.Add(step);

        HasCodeErrors = true;
        CodeValidationMessage = result.ErrorMessage ?? "";
        StatusMessage = "Code execution failed";

        ShowError($"Code execution error:\n{result.ErrorMessage}");
      }
    }
    catch (Exception ex)
    {
      var step = TransformationStep.CreateCodeStep(CSharpCode);
      step.StepNumber = TransformationSteps.Count + 1;
      step.MarkAsError(ex.Message);
      TransformationSteps.Add(step);

      HasCodeErrors = true;
      CodeValidationMessage = ex.Message;
      ShowError($"Unexpected error:\n{ex.Message}");
      StatusMessage = "Code execution failed";
    }
    finally
    {
      IsProcessing = false;
    }
  }

  [RelayCommand]
  private async Task ValidateCodeAsync()
  {
    if (string.IsNullOrWhiteSpace(CSharpCode))
    {
      HasCodeErrors = false;
      CodeValidationMessage = "";
      return;
    }

    try
    {
      StatusMessage = "Validating code...";

      var validation = await Task.Run(() => _transformationEngine.ValidateCode(CSharpCode));

      HasCodeErrors = !validation.IsValid;
      CodeValidationMessage = validation.ErrorMessage ?? "";

      StatusMessage = validation.IsValid ? "Code validation passed" : "Code validation failed";
    }
    catch (Exception ex)
    {
      HasCodeErrors = true;
      CodeValidationMessage = ex.Message;
      StatusMessage = "Code validation error";
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
      CSharpCode = @"// Write your C# transformation code here
// Available variable: data (DataTable)

// Examples:
// Filter rows: data = data.AsEnumerable().Where(row => row.Field<int>(""Age"") > 25).CopyToDataTable();
// Add column: data.Columns.Add(""Status"", typeof(string));
// Sort data: data.DefaultView.Sort = ""Name ASC""; data = data.DefaultView.ToTable();

";

      RowCount = 0;
      ColumnCount = 0;
      LastExecutionTime = "-";
      HasCodeErrors = false;
      CodeValidationMessage = "";
      StatusMessage = "Data cleared";

      _transformationEngine.ResetScriptState();
    }
  }

  [RelayCommand]
  private void SaveWorkspace()
  {
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

  [RelayCommand]
  private void InsertCodeTemplate(string templateKey)
  {
    var templates = _transformationEngine.GetCodeTemplates();
    if (templates.ContainsKey(templateKey))
    {
      var template = templates[templateKey];
      CSharpCode = template.Code;
      StatusMessage = $"Inserted template: {template.Name}";
    }
  }

  #endregion

  #region Constructor

  public MainViewModel()
  {
    _excelService = new ExcelDataService();
    _csvService = new CsvDataService();
    _fileDialogService = new FileDialogService();
    _transformationEngine = new CSharpTransformationEngine();

    LoadSampleData();
  }

  #endregion

  #region Private Methods

  private IDataService GetDataServiceForFile(string filePath)
  {
    if (_excelService.CanHandle(filePath))
      return _excelService;

    if (_csvService.CanHandle(filePath))
      return _csvService;

    throw new NotSupportedException($"File format not supported: {Path.GetExtension(filePath)}");
  }

  private void UpdateDataInfo(DataTable data)
  {
    RowCount = data.Rows.Count;
    ColumnCount = data.Columns.Count;
  }

  private void RenumberSteps()
  {
    for (int i = 0; i < TransformationSteps.Count; i++)
    {
      TransformationSteps[i].StepNumber = i + 1;
    }
  }

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

    StatusMessage = "Sample data loaded - ready to transform!";
  }

  private void ShowError(string message)
  {
    MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
  }

  private void ShowWarning(string message)
  {
    MessageBox.Show(message, "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
  }

  private void ShowInfo(string message)
  {
    MessageBox.Show(message, "Information", MessageBoxButton.OK, MessageBoxImage.Information);
  }

  #endregion

  #region Property Changed Handlers

  partial void OnCurrentDataChanged(DataTable? value)
  {
    if (value != null)
      UpdateDataInfo(value);
    else
    {
      RowCount = 0;
      ColumnCount = 0;
    }
  }

  #endregion

  #region Code Validation (Auto-triggered)

  partial void OnCSharpCodeChanged(string value)
  {
    // Validação automática com delay para não sobrecarregar
    Task.Delay(2000).ContinueWith(_ =>
    {
      if (CSharpCode == value) // Verificar se ainda é o mesmo código
      {
        System.Windows.Application.Current?.Dispatcher.BeginInvoke(() =>
        {
          try
          {
            if (ValidateCodeCommand.CanExecute(null))
              ValidateCodeCommand.Execute(null);
          }
          catch (Exception ex)
          {
            System.Diagnostics.Debug.WriteLine($"Auto-validation error: {ex.Message}");
          }
        });
      }
    });
  }

  #endregion
}