using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Highlighting;

namespace RealQuery.Views.UserControls;

/// <summary>
/// CodeEditor com AvalonEdit, templates e validação
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

  private bool _isUpdatingText;

  #endregion

  #region Constructor

  public CodeEditor()
  {
    InitializeComponent();
    InitializeEditor();
    InitializeEvents();
  }

  #endregion

  #region Initialization

  private void InitializeEditor()
  {
    // Configurar syntax highlighting
    AvalonEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("C#");

    // Configurar opções
    AvalonEditor.Options.ConvertTabsToSpaces = true;
    AvalonEditor.Options.IndentationSize = 2;
    AvalonEditor.Options.EnableHyperlinks = false;
    AvalonEditor.Options.EnableEmailHyperlinks = false;

    // Texto inicial
    if (string.IsNullOrEmpty(CodeText))
    {
      CodeText = @"// Write your C# transformation code here
// Available variable: data (DataTable)

// Examples:
// Filter: data = data.AsEnumerable().Where(row => row.Field<int>(""Age"") > 25).CopyToDataTable();
// Sort: data.DefaultView.Sort = ""Name ASC""; data = data.DefaultView.ToTable();

";
    }

    Loaded += (s, e) => AvalonEditor.Focus();
  }

  private void InitializeEvents()
  {
    // Eventos dos botões
    ExecuteButton.Click += (s, e) => ExecuteRequested?.Invoke(this, EventArgs.Empty);
    ValidateButton.Click += (s, e) => ValidateRequested?.Invoke(this, EventArgs.Empty);
    FormatButton.Click += (s, e) => FormatCode();
    TemplatesButton.Click += (s, e) => TemplatesButton.ContextMenu.IsOpen = true;

    // Eventos dos templates
    FilterTemplate.Click += (s, e) => TemplateRequested?.Invoke(this, "filter_rows");
    SortTemplate.Click += (s, e) => TemplateRequested?.Invoke(this, "sort_data");
    AddColumnTemplate.Click += (s, e) => TemplateRequested?.Invoke(this, "add_column");
    GroupTemplate.Click += (s, e) => TemplateRequested?.Invoke(this, "group_aggregate");
    RemoveDuplicatesTemplate.Click += (s, e) => TemplateRequested?.Invoke(this, "remove_duplicates");
    ConvertTypesTemplate.Click += (s, e) => TemplateRequested?.Invoke(this, "convert_types");

    // Eventos do editor
    AvalonEditor.TextChanged += OnEditorTextChanged;
    AvalonEditor.KeyDown += OnEditorKeyDown;
  }

  #endregion

  #region Event Handlers

  private void OnEditorTextChanged(object? sender, EventArgs e)
  {
    if (_isUpdatingText) return;

    _isUpdatingText = true;
    CodeText = AvalonEditor.Text;
    _isUpdatingText = false;
  }

  private void OnEditorKeyDown(object? sender, KeyEventArgs e)
  {
    if (e.Key == Key.F5)
    {
      ExecuteRequested?.Invoke(this, EventArgs.Empty);
      e.Handled = true;
    }
    else if (e.Key == Key.F7)
    {
      ValidateRequested?.Invoke(this, EventArgs.Empty);
      e.Handled = true;
    }
    else if (e.Key == Key.F && e.KeyboardDevice.Modifiers == ModifierKeys.Control)
    {
      FormatCode();
      e.Handled = true;
    }
  }

  #endregion

  #region Property Change Handlers

  private static void OnCodeTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
  {
    if (d is CodeEditor editor)
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
    AvalonEditor.Text = newText ?? string.Empty;
    _isUpdatingText = false;
  }

  private void UpdateErrorState(bool hasErrors)
  {
    if (hasErrors)
    {
      StatusIndicator.Fill = new SolidColorBrush(Colors.Red);
      StatusText.Text = "✗ Syntax Error";
      StatusText.Foreground = new SolidColorBrush(Colors.Red);
      ErrorPanel.Visibility = Visibility.Visible;
    }
    else
    {
      StatusIndicator.Fill = new SolidColorBrush(Colors.Green);
      StatusText.Text = "✓ Syntax OK";
      StatusText.Foreground = new SolidColorBrush(Colors.Green);
      ErrorPanel.Visibility = Visibility.Collapsed;
    }
  }

  private void UpdateErrorMessage(string message)
  {
    ErrorText.Text = message ?? "";
  }

  #endregion

  #region Code Operations

  private void FormatCode()
  {
    try
    {
      var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(CodeText);
      var root = tree.GetRoot();
      var formatted = root.NormalizeWhitespace();

      CodeText = formatted.ToFullString();

      StatusText.Text = "✓ Code formatted";
      StatusIndicator.Fill = new SolidColorBrush(Colors.Green);

      Task.Delay(2000).ContinueWith(_ => Dispatcher.Invoke(() =>
      {
        if (!HasErrors)
        {
          StatusText.Text = "✓ Syntax OK";
        }
      }));
    }
    catch (Exception ex)
    {
      StatusText.Text = "✗ Format Error";
      StatusIndicator.Fill = new SolidColorBrush(Colors.Orange);

      ToolTip = $"Format error: {ex.Message}";

      Task.Delay(3000).ContinueWith(_ => Dispatcher.Invoke(() =>
      {
        ToolTip = null;
        if (!HasErrors)
        {
          StatusText.Text = "✓ Syntax OK";
          StatusIndicator.Fill = new SolidColorBrush(Colors.Green);
        }
      }));
    }
  }

  #endregion

  #region Public Methods

  public void InsertTextAtCursor(string text)
  {
    if (string.IsNullOrEmpty(text)) return;

    var caretOffset = AvalonEditor.CaretOffset;
    AvalonEditor.Document.Insert(caretOffset, text);
    AvalonEditor.Focus();
  }

  public void SelectAll()
  {
    AvalonEditor.SelectAll();
    AvalonEditor.Focus();
  }

  public int GetCurrentLineNumber()
  {
    return AvalonEditor.Document.GetLineByOffset(AvalonEditor.CaretOffset).LineNumber;
  }

  public void GoToLine(int lineNumber)
  {
    if (lineNumber > 0 && lineNumber <= AvalonEditor.Document.LineCount)
    {
      var line = AvalonEditor.Document.GetLineByNumber(lineNumber);
      AvalonEditor.CaretOffset = line.Offset;
      AvalonEditor.ScrollToLine(lineNumber);
      AvalonEditor.Focus();
    }
  }

  #endregion
}