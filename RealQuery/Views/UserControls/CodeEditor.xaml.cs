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
/// CodeEditor com AvalonEdit
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
  private CompletionWindow? _completionWindow;

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
    try
    {
      // Carregar TrueBlue theme
      LoadTrueBlueTheme();

      // Configurar syntax highlighting para C#
      AvalonEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("C#");

      // Configurações básicas do AvalonEdit
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
      AvalonEditor.Options.EnableRectangularSelection = true;
      AvalonEditor.Options.ShowColumnRuler = false;

      // Event handlers
      AvalonEditor.TextChanged += AvalonEditor_TextChanged;
      AvalonEditor.TextArea.KeyDown += TextArea_KeyDown;

      // Auto-completion setup
      AvalonEditor.TextArea.TextEntered += TextArea_TextEntered;
      AvalonEditor.TextArea.TextEntering += TextArea_TextEntering;

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
    catch (Exception ex)
    {
      UpdateStatus("✗ Editor Error", Colors.Red);
      System.Diagnostics.Debug.WriteLine($"AvalonEdit initialization error: {ex.Message}");
    }
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

    // Ctrl+F - Format
    AvalonEditor.InputBindings.Add(new KeyBinding(
        new RelayCommand(_ => FormatCode()),
        new KeyGesture(Key.F, ModifierKeys.Control)));

    // Ctrl+/ - Comment/Uncomment
    AvalonEditor.InputBindings.Add(new KeyBinding(
        new RelayCommand(_ => ToggleComment()),
        new KeyGesture(Key.OemQuestion, ModifierKeys.Control)));

    // Ctrl+Space - IntelliSense
    AvalonEditor.InputBindings.Add(new KeyBinding(
        new RelayCommand(_ => ShowAutoCompletion()),
        new KeyGesture(Key.Space, ModifierKeys.Control)));
  }

  #endregion

  #region Theme Loading

  private void LoadTrueBlueTheme()
  {
    try
    {
      var themePath = System.IO.Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory,
        "Resources",
        "TrueBlue.xshd");

      if (System.IO.File.Exists(themePath))
      {
        using (var stream = System.IO.File.OpenRead(themePath))
        using (var reader = System.Xml.XmlReader.Create(stream))
        {
          var themeDefinition = ICSharpCode.AvalonEdit.Highlighting.Xshd.HighlightingLoader.LoadXshd(reader);
          var highlighting = ICSharpCode.AvalonEdit.Highlighting.Xshd.HighlightingLoader.Load(themeDefinition, HighlightingManager.Instance);
          HighlightingManager.Instance.RegisterHighlighting("C#", new[] { ".cs" }, highlighting);
        }
      }
    }
    catch (Exception ex)
    {
      System.Diagnostics.Debug.WriteLine($"Failed to load TrueBlue theme: {ex.Message}");
    }
  }

  #endregion

  #region Auto-completion

  private void TextArea_TextEntering(object? sender, TextCompositionEventArgs e)
  {
    if (e.Text.Length > 0 && _completionWindow != null)
    {
      if (!char.IsLetterOrDigit(e.Text[0]))
      {
        _completionWindow.CompletionList.RequestInsertion(e);
      }
    }
  }

  private void TextArea_TextEntered(object? sender, TextCompositionEventArgs e)
  {
    // Auto-trigger completion quando digita '.'
    if (e.Text == ".")
    {
      ShowAutoCompletion();
    }
  }

  private void ShowAutoCompletion()
  {
    try
    {
      var textArea = AvalonEditor.TextArea;
      var offset = textArea.Caret.Offset;

      // Verificar contexto (o que está antes do cursor)
      var wordBeforeDot = GetWordBeforeCursor(offset);

      _completionWindow = new CompletionWindow(textArea);
      var data = _completionWindow.CompletionList.CompletionData;

      // Sugestões baseadas no contexto
      if (wordBeforeDot == "data")
      {
        // Membros do DataTable
        data.Add(new MyCompletionData("Rows", "All rows in the table"));
        data.Add(new MyCompletionData("Columns", "All columns in the table"));
        data.Add(new MyCompletionData("AsEnumerable()", "Convert to LINQ enumerable"));
        data.Add(new MyCompletionData("Select()", "Select with expression"));
        data.Add(new MyCompletionData("DefaultView", "Default view of the table"));
        data.Add(new MyCompletionData("NewRow()", "Create a new row"));
        data.Add(new MyCompletionData("Clear()", "Clear all rows"));
        data.Add(new MyCompletionData("Clone()", "Clone table structure"));
        data.Add(new MyCompletionData("Copy()", "Copy table with data"));
      }
      else if (wordBeforeDot == "row")
      {
        // Membros do DataRow
        data.Add(new MyCompletionData("Field<T>(\"column\")", "Get typed field value"));
        data.Add(new MyCompletionData("SetField(\"column\", value)", "Set field value"));
        data.Add(new MyCompletionData("ItemArray", "All values in row"));
        data.Add(new MyCompletionData("Table", "Parent table"));
      }
      else if (wordBeforeDot.EndsWith("AsEnumerable()") || wordBeforeDot.Contains("Where") || wordBeforeDot.Contains("Select"))
      {
        // LINQ methods
        data.Add(new MyCompletionData("Where()", "Filter rows"));
        data.Add(new MyCompletionData("Select()", "Project rows"));
        data.Add(new MyCompletionData("OrderBy()", "Sort ascending"));
        data.Add(new MyCompletionData("OrderByDescending()", "Sort descending"));
        data.Add(new MyCompletionData("GroupBy()", "Group rows"));
        data.Add(new MyCompletionData("First()", "Get first element"));
        data.Add(new MyCompletionData("FirstOrDefault()", "Get first or default"));
        data.Add(new MyCompletionData("Count()", "Count elements"));
        data.Add(new MyCompletionData("Any()", "Check if any exists"));
        data.Add(new MyCompletionData("CopyToDataTable()", "Convert back to DataTable"));
      }
      else
      {
        // Sugestões gerais
        data.Add(new MyCompletionData("data", "Current DataTable"));
        data.Add(new MyCompletionData("row", "Current DataRow"));
        data.Add(new MyCompletionData("var", "Variable declaration"));
        data.Add(new MyCompletionData("foreach", "Foreach loop"));
        data.Add(new MyCompletionData("if", "If statement"));
        data.Add(new MyCompletionData("new", "New instance"));
        data.Add(new MyCompletionData("return", "Return statement"));
      }

      _completionWindow.Show();
      _completionWindow.Closed += (s, e) => _completionWindow = null;
    }
    catch (Exception ex)
    {
      System.Diagnostics.Debug.WriteLine($"Auto-completion error: {ex.Message}");
    }
  }

  private string GetWordBeforeCursor(int offset)
  {
    try
    {
      var document = AvalonEditor.Document;
      if (offset <= 0) return "";

      // Pegar os últimos 50 caracteres antes do cursor
      var start = Math.Max(0, offset - 50);
      var text = document.GetText(start, offset - start);

      // Encontrar a última palavra antes do ponto
      var lastDotIndex = text.LastIndexOf('.');
      if (lastDotIndex >= 0)
      {
        // Pegar tudo antes do último ponto
        text = text.Substring(0, lastDotIndex);
      }

      // Remover espaços e pegar última palavra
      text = text.TrimEnd();
      var match = System.Text.RegularExpressions.Regex.Match(text, @"(\w+(?:\.\w+|\(\))*)$");
      return match.Success ? match.Value : "";
    }
    catch
    {
      return "";
    }
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

  private void TextArea_KeyDown(object? sender, KeyEventArgs e)
  {
    // Interceptar teclas especiais se necessário
    if (e.Key == Key.Escape && _completionWindow != null)
    {
      _completionWindow.Close();
      e.Handled = true;
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
      var text = AvalonEditor.Text;
      text = System.Text.RegularExpressions.Regex.Replace(text, @"\n\s*\n\s*\n", "\n\n");

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

        if (trimmedLine.StartsWith("}"))
          indentLevel = Math.Max(0, indentLevel - 1);

        var indent = new string(' ', indentLevel * 2);
        formattedLines.Add(indent + trimmedLine);

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
        var line = AvalonEditor.Document.GetLineByOffset(textArea.Caret.Offset);
        var lineText = AvalonEditor.Document.GetText(line);

        if (lineText.Trim().StartsWith("//"))
        {
          var index = lineText.IndexOf("//");
          if (index >= 0)
          {
            var newText = lineText.Remove(index, 2);
            AvalonEditor.Document.Replace(line, newText);
          }
        }
        else
        {
          var leadingWhitespace = lineText.Length - lineText.TrimStart().Length;
          AvalonEditor.Document.Insert(line.Offset + leadingWhitespace, "// ");
        }
      }
      else
      {
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
      if (isDark)
      {
        AvalonEditor.LineNumbersForeground = new SolidColorBrush(Color.FromRgb(128, 128, 128));
      }
      else
      {
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

  public void SelectAll() => AvalonEditor.SelectAll();
  public void Copy() => AvalonEditor.Copy();
  public void Cut() => AvalonEditor.Cut();
  public void Paste() => AvalonEditor.Paste();
  public void Undo() { if (AvalonEditor.CanUndo) AvalonEditor.Undo(); }
  public void Redo() { if (AvalonEditor.CanRedo) AvalonEditor.Redo(); }

  #endregion

  #region Default Code

  private string GetDefaultCode()
  {
    return @"// Write your C# transformation code here
// Available variable: data (DataTable)
// Press Ctrl+Space for auto-completion

// Examples:
// Filter: data = data.AsEnumerable().Where(row => row.Field<int>(""Age"") > 25).CopyToDataTable();
// Sort: data.DefaultView.Sort = ""Name ASC""; data = data.DefaultView.ToTable();
// Add column: data.Columns.Add(""Status"", typeof(string));

";
  }

  #endregion

  #region Completion Data Helper

  private class MyCompletionData : ICompletionData
  {
    public MyCompletionData(string text, string description = "")
    {
      Text = text;
      Description = description;
    }

    public ImageSource? Image => null;
    public string Text { get; }
    public object Content => Text;
    public object Description { get; }
    public double Priority => 0;

    public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
    {
      // Simplesmente substituir o segmento pelo texto completo
      textArea.Document.Replace(completionSegment, Text);
    }
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