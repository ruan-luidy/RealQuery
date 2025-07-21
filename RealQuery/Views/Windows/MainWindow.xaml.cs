using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RealQuery.Core.Services;
using RealQuery.ViewModels;

namespace RealQuery.Views.Windows;

public partial class MainWindow : HandyControl.Controls.Window
{
  private MainViewModel? _viewModel;

  public MainWindow()
  {
    InitializeComponent();

    // Conectar ViewModel se não estiver definido no XAML
    if (DataContext == null)
    {
      _viewModel = new MainViewModel();
      DataContext = _viewModel;
    }

    Loaded += MainWindow_Loaded;
  }

  private void MainWindow_Loaded(object sender, RoutedEventArgs e)
  {
    // Conectar código do editor com ViewModel se necessário
    if (_viewModel != null && CSharpCodeEditor != null)
    {
      // Sincronizar código inicial
      CSharpCodeEditor.CodeText = _viewModel.CSharpCode;

      // Evento quando código muda no editor
      CSharpCodeEditor.AvalonEditor.TextChanged += (s, args) =>
      {
        _viewModel.CSharpCode = CSharpCodeEditor.CodeText;
      };

      // Botão execute no editor
      CSharpCodeEditor.ExecuteButton.Click += (s, args) =>
      {
        if (_viewModel.ExecuteCodeCommand.CanExecute(null))
          _viewModel.ExecuteCodeCommand.Execute(null);
      };
    }
  }
}