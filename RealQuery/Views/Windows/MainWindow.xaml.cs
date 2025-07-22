using System.Windows;
using RealQuery.ViewModels;

namespace RealQuery.Views.Windows;

public partial class MainWindow : HandyControl.Controls.Window
{
  private MainViewModel? _viewModel;

  public MainWindow()
  {
    InitializeComponent();
    Loaded += MainWindow_Loaded;
  }

  private void MainWindow_Loaded(object sender, RoutedEventArgs e)
  {
    _viewModel = DataContext as MainViewModel;

    if (_viewModel != null && CSharpCodeEditor != null)
    {
      SetupCodeEditorEvents();
      System.Diagnostics.Debug.WriteLine("MainWindow loaded with Monaco Editor integration");
    }
  }

  private void SetupCodeEditorEvents()
  {
    if (_viewModel == null || CSharpCodeEditor == null) return;

    // Conectar eventos do CodeEditor com ViewModel
    CSharpCodeEditor.ExecuteRequested += (s, args) =>
    {
      try
      {
        if (_viewModel.ExecuteCodeCommand.CanExecute(null))
          _viewModel.ExecuteCodeCommand.Execute(null);
      }
      catch (System.Exception ex)
      {
        System.Diagnostics.Debug.WriteLine($"Execute command error: {ex.Message}");
      }
    };

    CSharpCodeEditor.ValidateRequested += (s, args) =>
    {
      try
      {
        if (_viewModel.ValidateCodeCommand.CanExecute(null))
          _viewModel.ValidateCodeCommand.Execute(null);
      }
      catch (System.Exception ex)
      {
        System.Diagnostics.Debug.WriteLine($"Validate command error: {ex.Message}");
      }
    };

    CSharpCodeEditor.TemplateRequested += (s, templateKey) =>
    {
      try
      {
        if (!string.IsNullOrEmpty(templateKey) && _viewModel.InsertCodeTemplateCommand.CanExecute(templateKey))
          _viewModel.InsertCodeTemplateCommand.Execute(templateKey);
      }
      catch (System.Exception ex)
      {
        System.Diagnostics.Debug.WriteLine($"Template insert error: {ex.Message}");
      }
    };

    System.Diagnostics.Debug.WriteLine("Code editor events connected successfully");
  }
}