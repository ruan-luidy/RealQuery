using System.Windows;
using System.Windows.Controls;
using ICSharpCode.AvalonEdit.Highlighting;

namespace RealQuery.Views.UserControls;

/// <summary>
/// Interaction logic for CodeEditor.xaml
/// </summary>
public partial class CodeEditor : UserControl
{
  public CodeEditor()
  {
    InitializeComponent();
    InitializeEditor();
  }

  private void InitializeEditor()
  {
    // Configurar syntax highlighting para C#
    AvalonEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("C#");

    // Texto inicial
    AvalonEditor.Text = @"// Write your C# transformation code here
// Available variable: data (DataTable)

// Example:
// Filter rows where Age > 18
// data = data.AsEnumerable()
//     .Where(row => (int)row[""Age""] > 18)
//     .CopyToDataTable();

";

    // Focus
    Loaded += (s, e) => AvalonEditor.Focus();
  }

  /// <summary>
  /// Obtém ou define o texto do código
  /// </summary>
  public string CodeText
  {
    get => AvalonEditor?.Text ?? "";
    set
    {
      if (AvalonEditor != null)
        AvalonEditor.Text = value ?? "";
    }
  }
}