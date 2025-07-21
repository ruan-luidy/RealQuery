using System.Windows;
using RealQuery.ViewModels;

namespace RealQuery.Views.Windows;

public partial class MainWindow : HandyControl.Controls.Window
{
  public MainWindow()
  {
    InitializeComponent();

    // O DataContext já é definido no XAML, mas podemos acessar se necessário
    var viewModel = (MainViewModel)DataContext;

    // Qualquer inicialização adicional da view pode ser feita aqui
    // Por exemplo, configurar AvalonEdit quando implementarmos

    Loaded += MainWindow_Loaded;
  }

  /// <summary>
  /// Evento quando a window termina de carregar
  /// </summary>
  private void MainWindow_Loaded(object sender, RoutedEventArgs e)
  {
    // Aqui podemos fazer configurações que precisam da window carregada
    // Como inicializar o AvalonEdit ou outros controles específicos

    // Por enquanto, apenas log para debug
    System.Diagnostics.Debug.WriteLine("MainWindow loaded successfully with MVVM binding");
  }

  /// <summary>
  /// Método para futuras configurações do AvalonEdit
  /// </summary>
  private void InitializeCodeEditor()
  {
    // TODO: Configurar AvalonEdit quando implementarmos
    // var codeEditor = new ICSharpCode.AvalonEdit.TextEditor();
    // CodeEditorContainer.Child = codeEditor;
    // 
    // // Binding para o texto
    // var binding = new Binding("CSharpCode");
    // binding.Source = DataContext;
    // binding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
    // codeEditor.SetBinding(TextEditor.TextProperty, binding);
  }

  /// <summary>
  /// Override para cleanup se necessário
  /// </summary>
  protected override void OnClosed(EventArgs e)
  {
    // Cleanup se necessário
    base.OnClosed(e);
  }
}