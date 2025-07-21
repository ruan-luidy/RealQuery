using System.Data;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System.Text.RegularExpressions;

namespace RealQuery.Core.Services;

/// <summary>
/// Engine para executar código C# usando Roslyn
/// </summary>
public class CSharpTransformationEngine
{
  private readonly ScriptOptions _scriptOptions;

  public CSharpTransformationEngine()
  {
    _scriptOptions = ScriptOptions.Default
        .WithReferences(
            typeof(object).Assembly,                    // System
            typeof(Console).Assembly,                   // System.Console
            typeof(Enumerable).Assembly,               // System.Linq
            typeof(DataTable).Assembly,                // System.Data
            typeof(Regex).Assembly                     // System.Text.RegularExpressions
        )
        .WithImports(
            "System",
            "System.Linq",
            "System.Data",
            "System.Collections.Generic",
            "System.Text.RegularExpressions"
        );
  }

  /// <summary>
  /// Executa código C# com DataTable como contexto
  /// </summary>
  public async Task<TransformationResult> ExecuteAsync(string code, DataTable inputData)
  {
    if (string.IsNullOrWhiteSpace(code))
      throw new ArgumentException("Code cannot be empty", nameof(code));

    if (inputData == null)
      throw new ArgumentNullException(nameof(inputData));

    var startTime = DateTime.Now;

    try
    {
      // Preprocessar código
      var processedCode = PreprocessCode(code);

      // Validar sintaxe primeiro
      var syntaxErrors = ValidateSyntax(processedCode);
      if (syntaxErrors.Any())
      {
        return TransformationResult.CreateError(
            string.Join(Environment.NewLine, syntaxErrors),
            DateTime.Now - startTime
        );
      }

      // Criar cópia dos dados
      var data = inputData.Copy();

      // Contexto de execução
      var globals = new ScriptGlobals
      {
        data = data,
        Data = data,
        rows = data.Rows.Count,
        cols = data.Columns.Count
      };

      // Executar código
      var script = CSharpScript.Create(processedCode, _scriptOptions, typeof(ScriptGlobals));
      await script.RunAsync(globals);

      // Verificar resultado
      var resultData = globals.data ?? data;

      return TransformationResult.CreateSuccess(
          resultData,
          DateTime.Now - startTime,
          $"Success: {inputData.Rows.Count} → {resultData.Rows.Count} rows"
      );
    }
    catch (CompilationErrorException ex)
    {
      var errors = ex.Diagnostics
          .Where(d => d.Severity == DiagnosticSeverity.Error)
          .Select(d => $"Line {d.Location.GetLineSpan().StartLinePosition.Line + 1}: {d.GetMessage()}");

      return TransformationResult.CreateError(
          string.Join(Environment.NewLine, errors),
          DateTime.Now - startTime
      );
    }
    catch (Exception ex)
    {
      return TransformationResult.CreateError(
          $"Runtime Error: {ex.Message}",
          DateTime.Now - startTime
      );
    }
  }

  /// <summary>
  /// Valida sintaxe do código
  /// </summary>
  public ValidationResult ValidateCode(string code)
  {
    if (string.IsNullOrWhiteSpace(code))
      return ValidationResult.CreateValid();

    try
    {
      var processedCode = PreprocessCode(code);
      var syntaxErrors = ValidateSyntax(processedCode);

      if (syntaxErrors.Any())
      {
        return ValidationResult.CreateInvalid(string.Join(Environment.NewLine, syntaxErrors));
      }

      // Compilar sem executar
      var script = CSharpScript.Create(processedCode, _scriptOptions, typeof(ScriptGlobals));
      var compilation = script.GetCompilation();

      var diagnostics = compilation.GetDiagnostics()
          .Where(d => d.Severity == DiagnosticSeverity.Error)
          .ToList();

      if (diagnostics.Any())
      {
        var errors = diagnostics.Select(d => $"Line {d.Location.GetLineSpan().StartLinePosition.Line + 1}: {d.GetMessage()}");
        return ValidationResult.CreateInvalid(string.Join(Environment.NewLine, errors));
      }

      return ValidationResult.CreateValid();
    }
    catch (Exception ex)
    {
      return ValidationResult.CreateInvalid($"Validation Error: {ex.Message}");
    }
  }

  /// <summary>
  /// Obtém templates de código
  /// </summary>
  public Dictionary<string, CodeTemplate> GetCodeTemplates()
  {
    return new Dictionary<string, CodeTemplate>
    {
      ["filter_rows"] = new CodeTemplate
      {
        Name = "Filter Rows",
        Description = "Filter rows based on condition",
        Code = @"// Filter rows where condition is met
data = data.AsEnumerable()
    .Where(row => row.Field<int>(""Age"") > 18)
    .CopyToDataTable();"
      },

      ["sort_data"] = new CodeTemplate
      {
        Name = "Sort Data",
        Description = "Sort data by column",
        Code = @"// Sort data by column
data.DefaultView.Sort = ""Name ASC"";
data = data.DefaultView.ToTable();"
      },

      ["add_column"] = new CodeTemplate
      {
        Name = "Add Column",
        Description = "Add calculated column",
        Code = @"// Add new calculated column
data.Columns.Add(""FullName"", typeof(string));
foreach (DataRow row in data.Rows)
{
    row[""FullName""] = $""{row[""Name""]} - {row[""City""]}"";
}"
      },

      ["group_aggregate"] = new CodeTemplate
      {
        Name = "Group & Aggregate",
        Description = "Group data and calculate aggregates",
        Code = @"// Group by city and calculate aggregates
var grouped = data.AsEnumerable()
    .GroupBy(row => row.Field<string>(""City""))
    .Select(g => new {
        City = g.Key,
        Count = g.Count(),
        AvgSalary = g.Average(r => r.Field<decimal>(""Salary""))
    });

var result = new DataTable();
result.Columns.Add(""City"", typeof(string));
result.Columns.Add(""Count"", typeof(int));
result.Columns.Add(""AvgSalary"", typeof(decimal));

foreach (var item in grouped)
{
    result.Rows.Add(item.City, item.Count, Math.Round(item.AvgSalary, 2));
}
data = result;"
      },

      ["remove_duplicates"] = new CodeTemplate
      {
        Name = "Remove Duplicates",
        Description = "Remove duplicate rows",
        Code = @"// Remove duplicate rows based on Name
data = data.AsEnumerable()
    .GroupBy(row => row.Field<string>(""Name""))
    .Select(g => g.First())
    .CopyToDataTable();"
      },

      ["convert_types"] = new CodeTemplate
      {
        Name = "Convert Data Types",
        Description = "Convert column data types",
        Code = @"// Add 10% bonus to all salaries
foreach (DataRow row in data.Rows)
{
    decimal currentSalary = row.Field<decimal>(""Salary"");
    row[""Salary""] = Math.Round(currentSalary * 1.10m, 2);
}"
      }
    };
  }

  private string PreprocessCode(string code)
  {
    if (!code.Contains("data") && !code.Contains("Data"))
    {
      code = "// Access your data using 'data' variable\n" + code;
    }
    return code;
  }

  private List<string> ValidateSyntax(string code)
  {
    var errors = new List<string>();

    try
    {
      var tree = CSharpSyntaxTree.ParseText(code);
      var diagnostics = tree.GetDiagnostics();

      foreach (var diagnostic in diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error))
      {
        var line = diagnostic.Location.GetLineSpan().StartLinePosition.Line + 1;
        errors.Add($"Line {line}: {diagnostic.GetMessage()}");
      }
    }
    catch (Exception ex)
    {
      errors.Add($"Parse Error: {ex.Message}");
    }

    return errors;
  }

  public void ResetScriptState()
  {
    // Reset interno se necessário
  }
}

/// <summary>
/// Contexto global para scripts
/// </summary>
public class ScriptGlobals
{
  public DataTable data { get; set; } = new();
  public DataTable Data { get; set; } = new();
  public int rows { get; set; }
  public int cols { get; set; }

  public void Log(object message)
  {
    System.Diagnostics.Debug.WriteLine($"[Script] {message}");
  }

  public DataTable NewTable()
  {
    return new DataTable();
  }
}

/// <summary>
/// Resultado de transformação
/// </summary>
public class TransformationResult
{
  public bool Success { get; set; }
  public DataTable? ResultData { get; set; }
  public string Message { get; set; } = "";
  public string? ErrorMessage { get; set; }
  public TimeSpan ExecutionTime { get; set; }

  public static TransformationResult CreateSuccess(DataTable data, TimeSpan executionTime, string message = "")
  {
    return new TransformationResult
    {
      Success = true,
      ResultData = data,
      Message = message,
      ExecutionTime = executionTime
    };
  }

  public static TransformationResult CreateError(string errorMessage, TimeSpan executionTime)
  {
    return new TransformationResult
    {
      Success = false,
      ErrorMessage = errorMessage,
      ExecutionTime = executionTime
    };
  }
}

/// <summary>
/// Resultado de validação
/// </summary>
public class ValidationResult
{
  public bool IsValid { get; set; }
  public string? ErrorMessage { get; set; }

  public static ValidationResult CreateValid()
  {
    return new ValidationResult { IsValid = true };
  }

  public static ValidationResult CreateInvalid(string errorMessage)
  {
    return new ValidationResult { IsValid = false, ErrorMessage = errorMessage };
  }
}

/// <summary>
/// Template de código
/// </summary>
public class CodeTemplate
{
  public string Name { get; set; } = "";
  public string Description { get; set; } = "";
  public string Code { get; set; } = "";
}