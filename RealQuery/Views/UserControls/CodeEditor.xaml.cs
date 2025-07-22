using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;

namespace RealQuery.Views.UserControls;

/// <summary>
/// CodeEditor com AvalonEdit - 100% nativo WPF
/// </summary>
public partial class CodeEditor : UserControl
{
  #region Dependency Properties

  public static readonly DependencyProperty CodeTextProperty =
      DependencyProperty.Register(
          nameof(CodeText),
          typeof(string),
          typeof(CodeEditor),
          new FrameworkPropertyMetadata(
              string.Empty,
              FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
              OnCodeTextChanged));

  public string CodeText
  {
    get => (string)GetValue(CodeTextProperty);
    set => SetValue(CodeTextProperty, value);
  }

  public static readonly DependencyProperty HasErrorsProperty =
      DependencyProperty.Register(
          nameof(HasErrors),
          typeof(bool),
          typeof(CodeEditor),
          new PropertyMetadata(false, OnHasErrorsChanged));

  public bool HasErrors
  {
    get => (bool)GetValue(HasErrorsProperty);
    set => SetValue(HasErrorsProperty, value);
  }

  public static readonly DependencyProperty ValidationMessageProperty =
      DependencyProperty.Register(
          nameof(ValidationMessage),
          typeof(string),
          typeof(CodeEditor),
          new PropertyMetadata(string.Empty, OnValidationMessageChanged));

  public string ValidationMessage
  {
    get => (string)GetValue(ValidationMessageProperty);
    set => SetValue(ValidationMessageProperty, value);
  }

  #endregion

  #region Events

  public event EventHandler? ExecuteRequested;
  public event EventHandler? ValidateRequested;
  public event EventHandler<string>? TemplateRequested;

  #endregion

  #region Fields

  private bool _isUpdatingText = false;
  private bool _isDarkTheme = true;

  #endregion

  #region Constructor

  public CodeEditor()
  {
    InitializeComponent();
    InitializeEditor();
    InitializeEvents();
  }

  #endregion

  #region Editor Initialization

  private void InitializeEditor()
  {
    // Configurar syntax highlighting para C#
    AvalonEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("C#");

    // Configurações básicas
    AvalonEditor.Options.EnableHyperlinks = false;
    AvalonEditor.Options.EnableEmailHyperlinks = false;
    AvalonEditor.Options.ShowSpaces = false;
    AvalonEditor.Options.ShowTabs = false;
    AvalonEditor.Options.ShowEndOfLine = false;
    AvalonEditor.Options.ConvertTabsToSpaces = true;
    AvalonEditor.Options.IndentationSize = 2;
    AvalonEditor.Options.CutCopyWholeLine = true;
    AvalonEditor.Options.EnableVirtualSpace = false;
    AvalonEditor.Options.EnableTextDragDrop = true;
    AvalonEditor.Options.HighlightCurrentLine = true;

    // Event handlers para mudanças no texto
    AvalonEditor.TextChanged += AvalonEditor_TextChanged;

    // Configurar tema inicial
    ApplyTheme(_isDarkTheme);

    // Configurar código inicial
    if (string.IsNullOrEmpty(CodeText))
    {
      CodeText = GetDefaultCode();
    }
    AvalonEditor.Text = CodeText;

    // Keyboard shortcuts
    SetupKeyboardShortcuts();

    UpdateStatus("✓ AvalonEdit Ready", Colors.Green);
  }

  private void SetupKeyboardShortcuts()
  {
    // F5 - Execute
    AvalonEditor.InputBindings.Add(new KeyBinding(
        new RelayCommand(_ => ExecuteRequested?.Invoke(this, EventArgs.Empty)),
        new KeyGesture(Key.F5)));

    // F7 - Validate
    AvalonEditor.InputBindings.Add(new KeyBinding(
        new RelayCommand(_ => ValidateRequested?.Invoke(this, EventArgs.Empty)),
        new KeyGesture(Key.F7)));

    // Ctrl+F - Format (vamos implementar básico)
    AvalonEditor.InputBindings.Add(new KeyBinding(
        new RelayCommand(_ => FormatCode()),
        new KeyGesture(Key.F, ModifierKeys.Control)));

    // Ctrl+/ - Comment/Uncomment
    AvalonEditor.InputBindings.Add(new KeyBinding(
        new RelayCommand(_ => ToggleComment()),
        new KeyGesture(Key.OemQuestion, ModifierKeys.Control)));
  }

  #endregion

  #region Event Handlers

  private void InitializeEvents()
  {
    // Button events
    ExecuteButton.Click += (s, e) => ExecuteRequested?.Invoke(this, EventArgs.Empty);
    ValidateButton.Click += (s, e) => ValidateRequested?.Invoke(this, EventArgs.Empty);
    FormatButton.Click += (s, e) => FormatCode();
    ThemeToggleButton.Click += (s, e) => ToggleTheme();
    TemplatesButton.Click += (s, e) => TemplatesButton.ContextMenu.IsOpen = true;

    // Template events
    FilterTemplate.Click += (s, e) => TemplateRequested?.Invoke(this, "filter_rows");
    SortTemplate.Click += (s, e) => TemplateRequested?.Invoke(this, "sort_data");
    AddColumnTemplate.Click += (s, e) => TemplateRequested?.Invoke(this, "add_column");
    GroupTemplate.Click += (s, e) => TemplateRequested?.Invoke(this, "group_aggregate");
    RemoveDuplicatesTemplate.Click += (s, e) => TemplateRequested?.Invoke(this, "remove_duplicates");
    ConvertTypesTemplate.Click += (s, e) => TemplateRequested?.Invoke(this, "convert_types");
  }

  private void AvalonEditor_TextChanged(object? sender, EventArgs e)
  {
    if (!_isUpdatingText)
    {
      _isUpdatingText = true;
      try
      {
        CodeText = AvalonEditor.Text;
      }
      finally
      {
        _isUpdatingText = false;
      }
    }
  }

  #endregion

  #region Property Change Handlers

  private static void OnCodeTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
  {
    if (d is CodeEditor editor && !editor._isUpdatingText)
    {
      editor.UpdateEditorText((string)e.NewValue);
    }
  }

  private static void OnHasErrorsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
  {
    if (d is CodeEditor editor)
    {
      editor.UpdateErrorState((bool)e.NewValue);
    }
  }

  private static void OnValidationMessageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
  {
    if (d is CodeEditor editor)
    {
      editor.UpdateErrorMessage((string)e.NewValue);
    }
  }

  private void UpdateEditorText(string newText)
  {
    if (_isUpdatingText) return;

    _isUpdatingText = true;
    try
    {
      AvalonEditor.Text = newText ?? "";
    }
    finally
    {
      _isUpdatingText = false;
    }
  }

  private void UpdateErrorState(bool hasErrors)
  {
    if (hasErrors)
    {
      UpdateStatus("✗ Syntax Error", Colors.Red);
      ErrorPanel.Visibility = Visibility.Visible;
    }
    else
    {
      UpdateStatus("✓ Syntax OK", Colors.Green);
      ErrorPanel.Visibility = Visibility.Collapsed;
    }
  }

  private void UpdateErrorMessage(string message)
  {
    ErrorText.Text = message ?? "";
  }

  private void UpdateStatus(string text, Color color)
  {
    StatusText.Text = text;
    StatusIndicator.Fill = new SolidColorBrush(color);
    StatusText.Foreground = new SolidColorBrush(color);
  }

  #endregion

  #region Editor Operations

  private void FormatCode()
  {
    try
    {
      // Implementação básica de formatação
      var text = AvalonEditor.Text;

      // Remover linhas vazias excessivas
      text = System.Text.RegularExpressions.Regex.Replace(text, @"\n\s*\n\s*\n", "\n\n");

      // Normalizar indentação (básico)
      var lines = text.Split('\n');
      var formattedLines = new List<string>();
      int indentLevel = 0;

      foreach (var line in lines)
      {
        var trimmedLine = line.Trim();
        if (string.IsNullOrEmpty(trimmedLine))
        {
          formattedLines.Add("");
          continue;
        }

        // Diminuir indent para }
        if (trimmedLine.StartsWith("}"))
          indentLevel = Math.Max(0, indentLevel - 1);

        // Aplicar indentação
        var indent = new string(' ', indentLevel * 2);
        formattedLines.Add(indent + trimmedLine);

        // Aumentar indent para {
        if (trimmedLine.EndsWith("{"))
          indentLevel++;
      }

      _isUpdatingText = true;
      try
      {
        AvalonEditor.Text = string.Join("\n", formattedLines);
        CodeText = AvalonEditor.Text;
      }
      finally
      {
        _isUpdatingText = false;
      }

      UpdateStatus("✓ Code formatted", Colors.Green);
    }
    catch (Exception ex)
    {
      UpdateStatus("✗ Format Error", Colors.Orange);
      System.Diagnostics.Debug.WriteLine($"Format error: {ex.Message}");
    }
  }

  private void ToggleComment()
  {
    try
    {
      var textArea = AvalonEditor.TextArea;
      var selection = textArea.Selection;

      if (selection.IsEmpty)
      {
        // Comentar linha atual
        var line = AvalonEditor.Document.GetLineByOffset(textArea.Caret.Offset);
        var lineText = AvalonEditor.Document.GetText(line);

        if (lineText.Trim().StartsWith("//"))
        {
          // Descomentar
          var index = lineText.IndexOf("//");
          if (index >= 0)
          {
            var newText = lineText.Remove(index, 2);
            AvalonEditor.Document.Replace(line, newText);
          }
        }
        else
        {
          // Comentar
          var leadingWhitespace = lineText.Length - lineText.TrimStart().Length;
          AvalonEditor.Document.Insert(line.Offset + leadingWhitespace, "// ");
        }
      }
      else
      {
        // Comentar seleção
        var selectedText = AvalonEditor.SelectedText;
        var commentedText = "/* " + selectedText + " */";
        AvalonEditor.SelectedText = commentedText;
      }
    }
    catch (Exception ex)
    {
      System.Diagnostics.Debug.WriteLine($"Toggle comment error: {ex.Message}");
    }
  }

  private void ToggleTheme()
  {
    _isDarkTheme = !_isDarkTheme;
    ApplyTheme(_isDarkTheme);

    ThemeToggleButton.Content = _isDarkTheme ? "☀️" : "🌙";
    ThemeToggleButton.ToolTip = _isDarkTheme ? "Switch to light theme" : "Switch to dark theme";
  }

  private void ApplyTheme(bool isDark)
  {
    try
    {
      // As cores já vêm do HandyControl theme
      // Só precisamos ajustar algumas específicas do AvalonEdit se necessário

      if (isDark)
      {
        // Tema escuro já é aplicado pelo HandyControl
        AvalonEditor.LineNumbersForeground = new SolidColorBrush(Color.FromRgb(128, 128, 128));
      }
      else
      {
        // Tema claro
        AvalonEditor.LineNumbersForeground = new SolidColorBrush(Color.FromRgb(64, 64, 64));
      }
    }
    catch (Exception ex)
    {
      System.Diagnostics.Debug.WriteLine($"Theme error: {ex.Message}");
    }
  }

  #endregion

  #region Public Methods

  public void InsertTextAtCursor(string text)
  {
    if (string.IsNullOrEmpty(text)) return;

    try
    {
      var textArea = AvalonEditor.TextArea;
      var offset = textArea.Caret.Offset;
      AvalonEditor.Document.Insert(offset, text);
      textArea.Caret.Offset = offset + text.Length;
      AvalonEditor.Focus();
    }
    catch (Exception ex)
    {
      System.Diagnostics.Debug.WriteLine($"Insert text error: {ex.Message}");
    }
  }

  public void SelectAll()
  {
    AvalonEditor.SelectAll();
  }

  public void Copy()
  {
    AvalonEditor.Copy();
  }

  public void Cut()
  {
    AvalonEditor.Cut();
  }

  public void Paste()
  {
    AvalonEditor.Paste();
  }

  public void Undo()
  {
    if (AvalonEditor.CanUndo)
      AvalonEditor.Undo();
  }

  public void Redo()
  {
    if (AvalonEditor.CanRedo)
      AvalonEditor.Redo();
  }

  #endregion

  #region Default Code

  private string GetDefaultCode()
  {
    return @"// Write your C# transformation code here
// Available variable: data (DataTable)

// Examples:
// Filter: data = data.AsEnumerable().Where(row => row.Field<int>(""Age"") > 25).CopyToDataTable();
// Sort: data.DefaultView.Sort = ""Name ASC""; data = data.DefaultView.ToTable();
// Add column: data.Columns.Add(""Status"", typeof(string));

";
  }

  #endregion

  #region RelayCommand Helper

  private class RelayCommand : ICommand
  {
    private readonly Action<object?> _execute;
    private readonly Func<object?, bool>? _canExecute;

    public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
    {
      _execute = execute ?? throw new ArgumentNullException(nameof(execute));
      _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged
    {
      add { CommandManager.RequerySuggested += value; }
      remove { CommandManager.RequerySuggested -= value; }
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;
    public void Execute(object? parameter) => _execute(parameter);
  }

  #endregion
}