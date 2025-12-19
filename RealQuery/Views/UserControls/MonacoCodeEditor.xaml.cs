using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Web.WebView2.Core;
using System.Text.Json;

namespace RealQuery.Views.UserControls;

/// <summary>
/// CodeEditor usando Monaco Editor (VS Code)
/// </summary>
public partial class MonacoCodeEditor : UserControl
{
  #region Dependency Properties

  public static readonly DependencyProperty CodeTextProperty =
      DependencyProperty.Register(
          nameof(CodeText),
          typeof(string),
          typeof(MonacoCodeEditor),
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
          typeof(MonacoCodeEditor),
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
          typeof(MonacoCodeEditor),
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
  private bool _isMonacoReady = false;

  #endregion

  #region Constructor

  public MonacoCodeEditor()
  {
    InitializeComponent();
    InitializeEditor();
    InitializeEvents();
  }

  #endregion

  #region Editor Initialization

  private async void InitializeEditor()
  {
    try
    {
      await MonacoWebView.EnsureCoreWebView2Async();

      // Configurar mensagens do JavaScript
      MonacoWebView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;

      // Carregar o editor HTML
      var htmlPath = System.IO.Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory,
        "Resources",
        "Monaco",
        "editor.html");

      if (System.IO.File.Exists(htmlPath))
      {
        MonacoWebView.CoreWebView2.Navigate(new Uri(htmlPath).AbsoluteUri);
      }
      else
      {
        UpdateStatus("✗ Monaco HTML not found", Colors.Red);
      }
    }
    catch (Exception ex)
    {
      UpdateStatus("✗ Monaco Error", Colors.Red);
      System.Diagnostics.Debug.WriteLine($"Monaco initialization error: {ex.Message}");
    }
  }

  #endregion

  #region WebView2 Events

  private async void CoreWebView2_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
  {
    try
    {
      var json = e.WebMessageAsJson;
      var message = JsonSerializer.Deserialize<MonacoMessage>(json);

      if (message == null) return;

      switch (message.type)
      {
        case "ready":
          _isMonacoReady = true;
          UpdateStatus("✓ Monaco Ready", Colors.Green);

          // Definir conteúdo inicial se houver
          if (!string.IsNullOrEmpty(CodeText))
          {
            await SetEditorContentAsync(CodeText);
          }
          break;

        case "contentChanged":
          if (!_isUpdatingText && !string.IsNullOrEmpty(message.content))
          {
            _isUpdatingText = true;
            CodeText = message.content;
            _isUpdatingText = false;
          }
          break;

        case "execute":
          ExecuteRequested?.Invoke(this, EventArgs.Empty);
          break;

        case "validate":
          ValidateRequested?.Invoke(this, EventArgs.Empty);
          break;

        case "save":
          // Pode implementar salvamento aqui se necessário
          break;
      }
    }
    catch (Exception ex)
    {
      System.Diagnostics.Debug.WriteLine($"WebMessage error: {ex.Message}");
    }
  }

  #endregion

  #region Event Handlers

  private void InitializeEvents()
  {
    // Button events
    ExecuteButton.Click += (s, e) => ExecuteRequested?.Invoke(this, EventArgs.Empty);
    ValidateButton.Click += (s, e) => ValidateRequested?.Invoke(this, EventArgs.Empty);
    FormatButton.Click += async (s, e) => await FormatCodeAsync();
    ThemeToggleButton.Click += async (s, e) => await ToggleThemeAsync();
    TemplatesButton.Click += (s, e) => TemplatesButton.ContextMenu.IsOpen = true;

    // Template events
    FilterTemplate.Click += (s, e) => TemplateRequested?.Invoke(this, "filter_rows");
    SortTemplate.Click += (s, e) => TemplateRequested?.Invoke(this, "sort_data");
    AddColumnTemplate.Click += (s, e) => TemplateRequested?.Invoke(this, "add_column");
    GroupTemplate.Click += (s, e) => TemplateRequested?.Invoke(this, "group_aggregate");
    RemoveDuplicatesTemplate.Click += (s, e) => TemplateRequested?.Invoke(this, "remove_duplicates");
    ConvertTypesTemplate.Click += (s, e) => TemplateRequested?.Invoke(this, "convert_types");
  }

  #endregion

  #region Property Change Handlers

  private static async void OnCodeTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
  {
    if (d is MonacoCodeEditor editor && !editor._isUpdatingText)
    {
      await editor.SetEditorContentAsync((string)e.NewValue);
    }
  }

  private static void OnHasErrorsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
  {
    if (d is MonacoCodeEditor editor)
    {
      editor.UpdateErrorState((bool)e.NewValue);
    }
  }

  private static void OnValidationMessageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
  {
    if (d is MonacoCodeEditor editor)
    {
      editor.UpdateErrorMessage((string)e.NewValue);
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

  #region Monaco Operations

  private async Task SetEditorContentAsync(string content)
  {
    if (!_isMonacoReady || MonacoWebView.CoreWebView2 == null) return;

    try
    {
      var escapedContent = JsonSerializer.Serialize(content ?? "");
      await MonacoWebView.CoreWebView2.ExecuteScriptAsync($"setContent({escapedContent});");
    }
    catch (Exception ex)
    {
      System.Diagnostics.Debug.WriteLine($"SetContent error: {ex.Message}");
    }
  }

  public async Task<string> GetEditorContentAsync()
  {
    if (!_isMonacoReady || MonacoWebView.CoreWebView2 == null) return "";

    try
    {
      var result = await MonacoWebView.CoreWebView2.ExecuteScriptAsync("getContent();");
      return JsonSerializer.Deserialize<string>(result) ?? "";
    }
    catch (Exception ex)
    {
      System.Diagnostics.Debug.WriteLine($"GetContent error: {ex.Message}");
      return "";
    }
  }

  private async Task FormatCodeAsync()
  {
    if (!_isMonacoReady || MonacoWebView.CoreWebView2 == null) return;

    try
    {
      await MonacoWebView.CoreWebView2.ExecuteScriptAsync("formatCode();");
      UpdateStatus("✓ Code formatted", Colors.Green);
    }
    catch (Exception ex)
    {
      UpdateStatus("✗ Format Error", Colors.Orange);
      System.Diagnostics.Debug.WriteLine($"Format error: {ex.Message}");
    }
  }

  private async Task ToggleThemeAsync()
  {
    _isDarkTheme = !_isDarkTheme;

    if (!_isMonacoReady || MonacoWebView.CoreWebView2 == null) return;

    try
    {
      var theme = _isDarkTheme ? "vs-dark" : "vs";
      await MonacoWebView.CoreWebView2.ExecuteScriptAsync($"setTheme('{theme}');");

      ThemeToggleButton.Content = _isDarkTheme ? "☀️" : "🌙";
      ThemeToggleButton.ToolTip = _isDarkTheme ? "Switch to light theme" : "Switch to dark theme";
    }
    catch (Exception ex)
    {
      System.Diagnostics.Debug.WriteLine($"Toggle theme error: {ex.Message}");
    }
  }

  public async Task InsertTextAtCursorAsync(string text)
  {
    if (string.IsNullOrEmpty(text) || !_isMonacoReady || MonacoWebView.CoreWebView2 == null) return;

    try
    {
      var currentContent = await GetEditorContentAsync();
      var newContent = currentContent + "\n" + text;
      await SetEditorContentAsync(newContent);
    }
    catch (Exception ex)
    {
      System.Diagnostics.Debug.WriteLine($"Insert text error: {ex.Message}");
    }
  }

  #endregion

  #region Helper Classes

  private class MonacoMessage
  {
    public string type { get; set; } = "";
    public string? content { get; set; }
  }

  #endregion
}
