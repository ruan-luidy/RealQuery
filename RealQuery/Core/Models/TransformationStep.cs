using System.Data;

namespace RealQuery.Core.Models;

/// <summary>
/// Representa um passo de transformação no pipeline
/// </summary>
public partial class TransformationStep : ObservableObject
{
  [ObservableProperty]
  private int _stepNumber;

  [ObservableProperty]
  private string _title = "";

  [ObservableProperty]
  private string _description = "";

  [ObservableProperty]
  private string _code = "";

  [ObservableProperty]
  private DateTime _timestamp;

  [ObservableProperty]
  private TransformationType _type;

  [ObservableProperty]
  private bool _isExecuted;

  [ObservableProperty]
  private bool _hasError;

  [ObservableProperty]
  private string? _errorMessage;

  [ObservableProperty]
  private TimeSpan? _executionTime;

  [ObservableProperty]
  private int _inputRowCount;

  [ObservableProperty]
  private int _outputRowCount;

  // PROPRIEDADES COMPUTED PARA BINDING
  public string StatusIcon => GetStatusIcon();
  public string StatusColor => GetStatusColor();

  public TransformationStep()
  {
    Timestamp = DateTime.Now;
    Type = TransformationType.Custom;
  }

  public TransformationStep(string title, string description, string code = "")
      : this()
  {
    Title = title;
    Description = description;
    Code = code;
  }

  /// <summary>
  /// Cria um step de import
  /// </summary>
  public static TransformationStep CreateImportStep(string fileName, int rowCount)
  {
    return new TransformationStep
    {
      Title = $"Import: {fileName}",
      Description = $"Loaded {rowCount:N0} rows from {fileName}",
      Type = TransformationType.Import,
      IsExecuted = true,
      OutputRowCount = rowCount,
      ExecutionTime = TimeSpan.FromMilliseconds(100) // Placeholder
    };
  }

  /// <summary>
  /// Cria um step de export
  /// </summary>
  public static TransformationStep CreateExportStep(string fileName, int rowCount)
  {
    return new TransformationStep
    {
      Title = $"Export: {fileName}",
      Description = $"Exported {rowCount:N0} rows to {fileName}",
      Type = TransformationType.Export,
      IsExecuted = true,
      InputRowCount = rowCount,
      ExecutionTime = TimeSpan.FromMilliseconds(200) // Placeholder
    };
  }

  /// <summary>
  /// Cria um step de código C#
  /// </summary>
  public static TransformationStep CreateCodeStep(string code, int inputRows = 0, int outputRows = 0)
  {
    return new TransformationStep
    {
      Title = "C# Transformation",
      Description = GenerateCodeDescription(code),
      Code = code,
      Type = TransformationType.CSharpCode,
      InputRowCount = inputRows,
      OutputRowCount = outputRows
    };
  }

  /// <summary>
  /// Marca step como executado com sucesso
  /// </summary>
  public void MarkAsExecuted(TimeSpan executionTime, int outputRowCount)
  {
    IsExecuted = true;
    HasError = false;
    ErrorMessage = null;
    ExecutionTime = executionTime;
    OutputRowCount = outputRowCount;

    // Notify UI sobre mudanças nas computed properties
    OnPropertyChanged(nameof(StatusIcon));
    OnPropertyChanged(nameof(StatusColor));
  }

  /// <summary>
  /// Marca step como erro
  /// </summary>
  public void MarkAsError(string errorMessage)
  {
    IsExecuted = false;
    HasError = true;
    ErrorMessage = errorMessage;

    // Notify UI sobre mudanças nas computed properties
    OnPropertyChanged(nameof(StatusIcon));
    OnPropertyChanged(nameof(StatusColor));
  }

  /// <summary>
  /// Gera descrição baseada no código
  /// </summary>
  private static string GenerateCodeDescription(string code)
  {
    if (string.IsNullOrWhiteSpace(code))
      return "Empty transformation";

    var lines = code.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                   .Where(line => !line.Trim().StartsWith("//"))
                   .Take(2);

    return string.Join("; ", lines).Trim();
  }

  /// <summary>
  /// Obtém status visual do step
  /// </summary>
  public string GetStatusIcon()
  {
    if (HasError) return "❌";
    if (IsExecuted) return "✅";
    return "⏳";
  }

  /// <summary>
  /// Obtém cor do status
  /// </summary>
  public string GetStatusColor()
  {
    if (HasError) return "#ff4757";
    if (IsExecuted) return "#2ed573";
    return "#ffa502";
  }

  // Override dos property changed para notificar computed properties
  partial void OnIsExecutedChanged(bool value)
  {
    OnPropertyChanged(nameof(StatusIcon));
    OnPropertyChanged(nameof(StatusColor));
  }

  partial void OnHasErrorChanged(bool value)
  {
    OnPropertyChanged(nameof(StatusIcon));
    OnPropertyChanged(nameof(StatusColor));
  }
}