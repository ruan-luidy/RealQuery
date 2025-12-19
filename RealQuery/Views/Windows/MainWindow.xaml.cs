using System.Windows;
using RealQuery.ViewModels;
using RealQuery.Core.Models;

namespace RealQuery.Views.Windows;

public partial class MainWindow : HandyControl.Controls.Window
{
  private MainViewModel? _viewModel;

  public MainWindow()
  {
    InitializeComponent();

    // ViewModel já definido no XAML, mas vamos conectar eventos
    Loaded += MainWindow_Loaded;
  }

  private void MainWindow_Loaded(object sender, RoutedEventArgs e)
  {
    _viewModel = DataContext as MainViewModel;

    if (_viewModel != null && CSharpCodeEditor != null)
    {
      // Conectar eventos do CodeEditor com ViewModel
      CSharpCodeEditor.ExecuteRequested += (s, args) =>
      {
        if (_viewModel.ExecuteCodeCommand.CanExecute(null))
          _viewModel.ExecuteCodeCommand.Execute(null);
      };

      CSharpCodeEditor.ValidateRequested += (s, args) =>
      {
        if (_viewModel.ValidateCodeCommand.CanExecute(null))
          _viewModel.ValidateCodeCommand.Execute(null);
      };

      CSharpCodeEditor.TemplateRequested += (s, templateKey) =>
      {
        if (_viewModel.InsertCodeTemplateCommand.CanExecute(templateKey))
          _viewModel.InsertCodeTemplateCommand.Execute(templateKey);
      };

      // Binding manual para propriedades que precisam de sincronização
      _viewModel.PropertyChanged += (s, e) =>
      {
        switch (e.PropertyName)
        {
          case nameof(_viewModel.CSharpCode):
            if (CSharpCodeEditor.CodeText != _viewModel.CSharpCode)
              CSharpCodeEditor.CodeText = _viewModel.CSharpCode;
            break;

          case nameof(_viewModel.HasCodeErrors):
            CSharpCodeEditor.HasErrors = _viewModel.HasCodeErrors;
            break;

          case nameof(_viewModel.CodeValidationMessage):
            CSharpCodeEditor.ValidationMessage = _viewModel.CodeValidationMessage;
            break;
        }
      };

      // Binding reverso - quando CodeEditor muda, atualiza ViewModel
      CSharpCodeEditor.GetBindingExpression(Views.UserControls.CodeEditor.CodeTextProperty)?.UpdateSource();
    }

    System.Diagnostics.Debug.WriteLine("MainWindow loaded with Roslyn + AvalonEdit integration");
  }

  #region PipelineCanvas Event Handlers

  private void PipelineCanvas_StepSelected(object? sender, TransformationStep e)
  {
    if (_viewModel != null)
    {
      _viewModel.SelectedStep = e;

      // Se o step tem código, carrega no editor
      if (!string.IsNullOrWhiteSpace(e.Code))
      {
        _viewModel.CSharpCode = e.Code;
      }
    }
  }

  private void PipelineCanvas_StepDeleted(object? sender, TransformationStep e)
  {
    if (_viewModel?.TransformationSteps != null)
    {
      _viewModel.TransformationSteps.Remove(e);

      // Renumerar os steps
      for (int i = 0; i < _viewModel.TransformationSteps.Count; i++)
      {
        _viewModel.TransformationSteps[i].StepNumber = i + 1;
      }
    }
  }

  #endregion
}