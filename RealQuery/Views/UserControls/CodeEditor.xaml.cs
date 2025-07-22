using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Web.WebView2.Core;
using System.Text.Json;

namespace RealQuery.Views.UserControls;

/// <summary>
/// CodeEditor com Monaco Editor via WebView2
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

  private bool _isEditorReady = false;
  private bool _isUpdatingText = false;
  private bool _isDarkTheme = true;

  #endregion

  #region Constructor

  public CodeEditor()
  {
    InitializeComponent();
    InitializeEvents();
  }

  #endregion

  #region WebView2 & Monaco Initialization

  private async void MonacoWebView_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
  {
    try
    {
      if (e.IsSuccess && MonacoWebView.CoreWebView2 != null)
      {
        await InitializeMonacoEditor();
      }
    }
    catch (Exception ex)
    {
      System.Diagnostics.Debug.WriteLine($"Monaco initialization error: {ex.Message}");
      StatusText.Text = "✗ Monaco Error";
      StatusIndicator.Fill = new SolidColorBrush(Colors.Red);
    }
  }

  private async Task InitializeMonacoEditor()
  {
    try
    {
      if (MonacoWebView.CoreWebView2 == null) return;

      // Register JS-C# communication
      MonacoWebView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;

      // Wait for Monaco to load
      await Task.Delay(1500);

      // Set initial code
      if (!string.IsNullOrEmpty(CodeText))
      {
        await SetEditorValue(CodeText);
      }
      else
      {
        await SetEditorValue(GetDefaultCode());
      }

      _isEditorReady = true;

      // Setup keyboard shortcuts
      await MonacoWebView.CoreWebView2.ExecuteScriptAsync(@"
        if (typeof editor !== 'undefined' && editor) {
          editor.addCommand(monaco.KeyCode.F5, function() {
            window.chrome.webview.postMessage({ type: 'execute' });
          });

          editor.addCommand(monaco.KeyCode.F7, function() {
            window.chrome.webview.postMessage({ type: 'validate' });
          });

          editor.addCommand(monaco.KeyMod.CtrlCmd | monaco.KeyCode.KeyF, function() {
            window.chrome.webview.postMessage({ type: 'format' });
          });
        }
      ");

      StatusText.Text = "✓ Monaco Ready";
      StatusIndicator.Fill = new SolidColorBrush(Colors.Green);
    }
    catch (Exception ex)
    {
      StatusText.Text = "✗ Monaco Error";
      StatusIndicator.Fill = new SolidColorBrush(Colors.Red);
      System.Diagnostics.Debug.WriteLine($"Monaco setup error: {ex.Message}");
    }
  }

  private string GetMonacoHtml()
  {
    return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ margin: 0; padding: 0; overflow: hidden; }}
        #container {{ height: 100vh; }}
    </style>
</head>
<body>
    <div id='container'></div>
    
    <script src='https://unpkg.com/monaco-editor@latest/min/vs/loader.js'></script>
    <script>
        require.config({{ paths: {{ vs: 'https://unpkg.com/monaco-editor@latest/min/vs' }} }});
        
        let editor;
        
        require(['vs/editor/editor.main'], function() {{
            editor = monaco.editor.create(document.getElementById('container'), {{
                value: '',
                language: 'csharp',
                theme: '{(_isDarkTheme ? "vs-dark" : "vs-light")}',
                fontSize: 14,
                fontFamily: 'Consolas, Cascadia Code, Monaco, monospace',
                minimap: {{ enabled: true }},
                scrollBeyondLastLine: false,
                automaticLayout: true,
                wordWrap: 'on',
                lineNumbers: 'on',
                renderWhitespace: 'selection',
                bracketMatching: 'always',
                autoIndent: 'full',
                formatOnPaste: true,
                formatOnType: true,
                suggest: {{
                    showKeywords: true,
                    showSnippets: true
                }}
            }});

            // Listen for content changes
            editor.onDidChangeModelContent(function(e) {{
                const content = editor.getValue();
                window.chrome.webview.postMessage({{ 
                    type: 'contentChanged', 
                    content: content 
                }});
            }});

            // Editor ready
            window.chrome.webview.postMessage({{ type: 'ready' }});
        }});

        // Expose functions to C#
        window.setEditorValue = function(value) {{
            if (editor) {{
                editor.setValue(value);
            }}
        }};

        window.getEditorValue = function() {{
            return editor ? editor.getValue() : '';
        }};

        window.formatCode = function() {{
            if (editor) {{
                editor.getAction('editor.action.formatDocument').run();
            }}
        }};

        window.setTheme = function(theme) {{
            if (editor) {{
                monaco.editor.setTheme(theme);
            }}
        }};

        window.insertText = function(text) {{
            if (editor) {{
                const selection = editor.getSelection();
                const op = {{ range: selection, text: text, forceMoveMarkers: true }};
                editor.executeEdits('insertText', [op]);
                editor.focus();
            }}
        }};
    </script>
</body>
</html>";
  }

  private async void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
  {
    try
    {
      var message = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(e.TryGetWebMessageAsString());

      if (message == null) return;

      var type = message["type"].GetString();

      switch (type)
      {
        case "ready":
          _isEditorReady = true;
          break;

        case "contentChanged":
          if (!_isUpdatingText && message.ContainsKey("content"))
          {
            var content = message["content"].GetString() ?? "";
            _isUpdatingText = true;
            CodeText = content;
            _isUpdatingText = false;
          }
          break;

        case "execute":
          ExecuteRequested?.Invoke(this, EventArgs.Empty);
          break;

        case "validate":
          ValidateRequested?.Invoke(this, EventArgs.Empty);
          break;

        case "format":
          await FormatCode();
          break;
      }
    }
    catch (Exception ex)
    {
      System.Diagnostics.Debug.WriteLine($"WebMessage error: {ex.Message}");
    }
  }

  #endregion

  #region Events Initialization

  private void InitializeEvents()
  {
    // Button events
    ExecuteButton.Click += (s, e) => ExecuteRequested?.Invoke(this, EventArgs.Empty);
    ValidateButton.Click += (s, e) => ValidateRequested?.Invoke(this, EventArgs.Empty);
    FormatButton.Click += async (s, e) => await FormatCode();
    ThemeToggleButton.Click += async (s, e) => await ToggleTheme();
    TemplatesButton.Click += (s, e) => TemplatesButton.ContextMenu.IsOpen = true;

    // Template events
    FilterTemplate.Click += (s, e) => TemplateRequested?.Invoke(this, "filter_rows");
    SortTemplate.Click += (s, e) => TemplateRequested?.Invoke(this, "sort_data");
    AddColumnTemplate.Click += (s, e) => TemplateRequested?.Invoke(this, "add_column");
    GroupTemplate.Click += (s, e) => TemplateRequested?.Invoke(this, "group_aggregate");
    RemoveDuplicatesTemplate.Click += (s, e) => TemplateRequested?.Invoke(this, "remove_duplicates");
    ConvertTypesTemplate.Click += (s, e) => TemplateRequested?.Invoke(this, "convert_types");

    Loaded += async (s, e) => {
      try
      {
        await MonacoWebView.EnsureCoreWebView2Async();
        await Task.Delay(100);
        MonacoWebView.NavigateToString(GetMonacoHtml());
      }
      catch (Exception ex)
      {
        System.Diagnostics.Debug.WriteLine($"WebView2 init error: {ex.Message}");
        StatusText.Text = "✗ WebView2 Error";
        StatusIndicator.Fill = new SolidColorBrush(Colors.Red);
      }
    };
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

  private async void UpdateEditorText(string newText)
  {
    if (!_isEditorReady || _isUpdatingText) return;

    try
    {
      _isUpdatingText = true;
      await SetEditorValue(newText ?? "");
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

  #region Monaco Operations

  private async Task SetEditorValue(string value)
  {
    if (!_isEditorReady || MonacoWebView.CoreWebView2 == null) return;

    try
    {
      var escapedValue = JsonSerializer.Serialize(value);
      await MonacoWebView.CoreWebView2.ExecuteScriptAsync($"if (typeof window.setEditorValue === 'function') window.setEditorValue({escapedValue});");
    }
    catch (Exception ex)
    {
      System.Diagnostics.Debug.WriteLine($"SetEditorValue error: {ex.Message}");
    }
  }

  private async Task<string> GetEditorValue()
  {
    if (!_isEditorReady || MonacoWebView.CoreWebView2 == null) return "";

    try
    {
      var result = await MonacoWebView.CoreWebView2.ExecuteScriptAsync("typeof window.getEditorValue === 'function' ? window.getEditorValue() : ''");
      return JsonSerializer.Deserialize<string>(result) ?? "";
    }
    catch (Exception ex)
    {
      System.Diagnostics.Debug.WriteLine($"GetEditorValue error: {ex.Message}");
      return "";
    }
  }

  private async Task FormatCode()
  {
    if (!_isEditorReady || MonacoWebView.CoreWebView2 == null) return;

    try
    {
      await MonacoWebView.CoreWebView2.ExecuteScriptAsync("if (typeof window.formatCode === 'function') window.formatCode();");
      StatusText.Text = "✓ Code formatted";
      StatusIndicator.Fill = new SolidColorBrush(Colors.Green);
    }
    catch (Exception ex)
    {
      StatusText.Text = "✗ Format Error";
      StatusIndicator.Fill = new SolidColorBrush(Colors.Orange);
      System.Diagnostics.Debug.WriteLine($"Format error: {ex.Message}");
    }
  }

  private async Task ToggleTheme()
  {
    if (!_isEditorReady || MonacoWebView.CoreWebView2 == null) return;

    try
    {
      _isDarkTheme = !_isDarkTheme;
      var theme = _isDarkTheme ? "vs-dark" : "vs-light";

      await MonacoWebView.CoreWebView2.ExecuteScriptAsync($"if (typeof window.setTheme === 'function') window.setTheme('{theme}');");

      ThemeToggleButton.Content = _isDarkTheme ? "☀️" : "🌙";
      ThemeToggleButton.ToolTip = _isDarkTheme ? "Switch to light theme" : "Switch to dark theme";
    }
    catch (Exception ex)
    {
      System.Diagnostics.Debug.WriteLine($"Theme toggle error: {ex.Message}");
    }
  }

  #endregion

  #region Public Methods

  public async Task InsertTextAtCursor(string text)
  {
    if (!_isEditorReady || string.IsNullOrEmpty(text) || MonacoWebView.CoreWebView2 == null) return;

    try
    {
      var escapedText = JsonSerializer.Serialize(text);
      await MonacoWebView.CoreWebView2.ExecuteScriptAsync($"if (typeof window.insertText === 'function') window.insertText({escapedText});");
    }
    catch (Exception ex)
    {
      System.Diagnostics.Debug.WriteLine($"Insert text error: {ex.Message}");
    }
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
}