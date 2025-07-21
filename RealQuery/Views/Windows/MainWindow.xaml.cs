using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RealQuery.Core.Services;

namespace RealQuery.Views.Windows;

public partial class MainWindow : HandyControl.Controls.Window
{
  private readonly ExcelDataService _excelService;
  private readonly FileDialogService _fileDialogService;
  private DataTable? _currentData;

  public MainWindow()
  {
    InitializeComponent();

    // Inicializar serviços
    _excelService = new ExcelDataService();
    _fileDialogService = new FileDialogService();

    // Carregar dados de exemplo iniciais
    LoadSampleData();

    // Conectar eventos dos botões
    ConnectEvents();
  }

  /// <summary>
  /// Conecta eventos dos botões do toolbar
  /// </summary>
  private void ConnectEvents()
  {
    // Encontrar botões no NonClientAreaContent
    var toolbar = ((Grid)this.NonClientAreaContent).Children.OfType<StackPanel>().FirstOrDefault();
    if (toolbar != null)
    {
      var buttons = toolbar.Children.OfType<System.Windows.Controls.Button>().ToArray();

      if (buttons.Length >= 5)
      {
        buttons[0].Click += ImportButton_Click;    // Import
        buttons[1].Click += ExportButton_Click;    // Export
        buttons[2].Click += ExecuteButton_Click;   // Execute
        buttons[3].Click += ClearButton_Click;     // Clear
        buttons[4].Click += SaveButton_Click;      // Save
      }
    }
  }

  /// <summary>
  /// Evento do botão Import
  /// </summary>
  private async void ImportButton_Click(object sender, RoutedEventArgs e)
  {
    try
    {
      UpdateStatus("Selecionando arquivo...");

      var filePath = _fileDialogService.OpenFileDialog(
          "Importar dados do Excel",
          _fileDialogService.GetExcelFilter()
      );

      if (string.IsNullOrEmpty(filePath))
        return;

      UpdateStatus("Importando dados...");

      // Validar arquivo
      if (!_fileDialogService.ValidateFile(filePath, out string errorMessage))
      {
        HandyControl.Controls.MessageBox.Show(
            $"Erro ao validar arquivo:\n{errorMessage}",
            "Erro",
            MessageBoxButton.OK,
            MessageBoxImage.Error
        );
        UpdateStatus("Erro na importação");
        return;
      }

      // Importar dados
      _currentData = await _excelService.ImportAsync(filePath);

      // Atualizar interface
      DataPreviewGrid.ItemsSource = _currentData.DefaultView;
      UpdateDataInfo(_currentData);
      AddTransformationStep($"Import: {Path.GetFileName(filePath)}", $"Loaded {_currentData.Rows.Count:N0} rows");

      UpdateStatus($"Importação concluída: {_currentData.Rows.Count:N0} linhas carregadas");
    }
    catch (Exception ex)
    {
      HandyControl.Controls.MessageBox.Show(
          $"Erro ao importar arquivo:\n{ex.Message}",
          "Erro de Importação",
          MessageBoxButton.OK,
          MessageBoxImage.Error
      );
      UpdateStatus("Erro na importação");
    }
  }

  /// <summary>
  /// Evento do botão Export
  /// </summary>
  private async void ExportButton_Click(object sender, RoutedEventArgs e)
  {
    if (_currentData == null || _currentData.Rows.Count == 0)
    {
      HandyControl.Controls.MessageBox.Show(
          "Não há dados para exportar.\nImporte ou gere dados primeiro.",
          "Aviso",
          MessageBoxButton.OK,
          MessageBoxImage.Warning
      );
      return;
    }

    try
    {
      UpdateStatus("Selecionando local para exportar...");

      var defaultFileName = $"RealQuery_Export_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
      var filePath = _fileDialogService.SaveFileDialog(
          "Exportar dados para Excel",
          defaultFileName,
          _fileDialogService.GetExcelFilter()
      );

      if (string.IsNullOrEmpty(filePath))
        return;

      UpdateStatus("Exportando dados...");

      // Validar caminho de saída
      if (!_fileDialogService.ValidateOutputPath(filePath, out string errorMessage))
      {
        HandyControl.Controls.MessageBox.Show(
            $"Erro ao validar caminho de saída:\n{errorMessage}",
            "Erro",
            MessageBoxButton.OK,
            MessageBoxImage.Error
        );
        UpdateStatus("Erro na exportação");
        return;
      }

      // Exportar dados
      await _excelService.ExportAsync(_currentData, filePath);

      AddTransformationStep($"Export: {Path.GetFileName(filePath)}", $"Exported {_currentData.Rows.Count:N0} rows");
      UpdateStatus($"Exportação concluída: {Path.GetFileName(filePath)}");

      // Perguntar se quer abrir o arquivo
      var result = HandyControl.Controls.MessageBox.Show(
          $"Arquivo exportado com sucesso!\n\n{filePath}\n\nDeseja abrir o arquivo?",
          "Exportação Concluída",
          MessageBoxButton.YesNo,
          MessageBoxImage.Information
      );

      if (result == MessageBoxResult.Yes)
      {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
          FileName = filePath,
          UseShellExecute = true
        });
      }
    }
    catch (Exception ex)
    {
      HandyControl.Controls.MessageBox.Show(
          $"Erro ao exportar arquivo:\n{ex.Message}",
          "Erro de Exportação",
          MessageBoxButton.OK,
          MessageBoxImage.Error
      );
      UpdateStatus("Erro na exportação");
    }
  }

  /// <summary>
  /// Evento do botão Execute (placeholder por enquanto)
  /// </summary>
  private void ExecuteButton_Click(object sender, RoutedEventArgs e)
  {
    UpdateStatus("Execução de código C# será implementada em breve...");

    HandyControl.Controls.MessageBox.Show(
        "Funcionalidade em desenvolvimento!\n\nEm breve você poderá executar código C# para transformar os dados.",
        "Em Breve",
        MessageBoxButton.OK,
        MessageBoxImage.Information
    );
  }

  /// <summary>
  /// Evento do botão Clear
  /// </summary>
  private void ClearButton_Click(object sender, RoutedEventArgs e)
  {
    var result = HandyControl.Controls.MessageBox.Show(
        "Tem certeza que deseja limpar todos os dados e transformações?",
        "Confirmar Limpeza",
        MessageBoxButton.YesNo,
        MessageBoxImage.Question
    );

    if (result == MessageBoxResult.Yes)
    {
      ClearAll();
      UpdateStatus("Dados limpos");
    }
  }

  /// <summary>
  /// Evento do botão Save (placeholder por enquanto)
  /// </summary>
  private void SaveButton_Click(object sender, RoutedEventArgs e)
  {
    UpdateStatus("Salvamento de workspace será implementado em breve...");

    HandyControl.Controls.MessageBox.Show(
        "Funcionalidade em desenvolvimento!\n\nEm breve você poderá salvar e carregar workspaces.",
        "Em Breve",
        MessageBoxButton.OK,
        MessageBoxImage.Information
    );
  }

  /// <summary>
  /// Limpa todos os dados e interface
  /// </summary>
  private void ClearAll()
  {
    _currentData = null;
    DataPreviewGrid.ItemsSource = null;
    StepsListBox.Items.Clear();
    CodeEditorPlaceholder.Text = "// Write your C# transformation code here\n// Example:\n// data = data.Where(row => row[\"Age\"] > 18);";

    StatusRowCount.Text = "Rows: 0";
    StatusColumnCount.Text = "Columns: 0";
    StatusExecutionTime.Text = "Last execution: -";
  }

  /// <summary>
  /// Carrega dados de exemplo para demonstração
  /// </summary>
  private void LoadSampleData()
  {
    var sampleData = new DataTable();
    sampleData.Columns.Add("ID", typeof(int));
    sampleData.Columns.Add("Name", typeof(string));
    sampleData.Columns.Add("Age", typeof(int));
    sampleData.Columns.Add("City", typeof(string));
    sampleData.Columns.Add("Salary", typeof(decimal));

    sampleData.Rows.Add(1, "João Silva", 28, "São Paulo", 5500.00m);
    sampleData.Rows.Add(2, "Maria Santos", 32, "Rio de Janeiro", 6200.00m);
    sampleData.Rows.Add(3, "Carlos Oliveira", 25, "Belo Horizonte", 4800.00m);
    sampleData.Rows.Add(4, "Ana Costa", 35, "Porto Alegre", 7100.00m);
    sampleData.Rows.Add(5, "Pedro Lima", 29, "Recife", 5300.00m);
    sampleData.Rows.Add(6, "Lucia Ferreira", 31, "Salvador", 5900.00m);
    sampleData.Rows.Add(7, "Roberto Alves", 27, "Fortaleza", 5100.00m);
    sampleData.Rows.Add(8, "Fernanda Rocha", 33, "Brasília", 6800.00m);

    _currentData = sampleData;
    DataPreviewGrid.ItemsSource = sampleData.DefaultView;
    UpdateDataInfo(sampleData);

    AddTransformationStep("Sample Data", "Loaded sample dataset with 8 rows");
  }

  /// <summary>
  /// Atualiza informações dos dados no status bar
  /// </summary>
  private void UpdateDataInfo(DataTable data)
  {
    StatusRowCount.Text = $"Rows: {data.Rows.Count:N0}";
    StatusColumnCount.Text = $"Columns: {data.Columns.Count}";
  }

  /// <summary>
  /// Atualiza status na barra inferior
  /// </summary>
  private void UpdateStatus(string message)
  {
    // Encontrar StatusBar no Grid
    var mainGrid = this.Content as Grid;
    if (mainGrid != null)
    {
      var statusBar = mainGrid.Children.OfType<System.Windows.Controls.Primitives.StatusBar>().FirstOrDefault();
      if (statusBar?.Items.Count > 0 && statusBar.Items[0] is StatusBarItem firstItem)
      {
        if (firstItem.Content is TextBlock textBlock)
          textBlock.Text = message;
      }
    }
  }

  /// <summary>
  /// Adiciona step na lista de transformações
  /// </summary>
  private void AddTransformationStep(string title, string description)
  {
    var listBoxItem = new ListBoxItem();
    var card = new HandyControl.Controls.Card();
    var stackPanel = new StackPanel();

    var titleBlock = new TextBlock
    {
      Text = $"{StepsListBox.Items.Count + 1}. {title}",
      FontWeight = FontWeights.Bold
    };

    var descriptionBlock = new TextBlock
    {
      Text = description,
      FontSize = 11,
      Foreground = Brushes.Gray
    };

    stackPanel.Children.Add(titleBlock);
    stackPanel.Children.Add(descriptionBlock);
    card.Content = stackPanel;
    listBoxItem.Content = card;

    StepsListBox.Items.Add(listBoxItem);

    // Auto-scroll para o último item
    StepsListBox.ScrollIntoView(listBoxItem);
  }
}