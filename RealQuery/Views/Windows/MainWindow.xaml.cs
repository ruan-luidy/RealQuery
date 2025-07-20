using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace RealQuery.Views.Windows;

public partial class MainWindow : HandyControl.Controls.Window
{
  public MainWindow()
  {
    InitializeComponent();
    LoadSampleData();
  }

  /// <summary>
  /// Carrega dados de exemplo para demonstração
  /// </summary>
  private void LoadSampleData()
  {
    // Criar DataTable de exemplo
    var sampleData = new DataTable();
    sampleData.Columns.Add("ID", typeof(int));
    sampleData.Columns.Add("Name", typeof(string));
    sampleData.Columns.Add("Age", typeof(int));
    sampleData.Columns.Add("City", typeof(string));
    sampleData.Columns.Add("Salary", typeof(decimal));

    // Adicionar dados de exemplo
    sampleData.Rows.Add(1, "João Silva", 28, "São Paulo", 5500.00m);
    sampleData.Rows.Add(2, "Maria Santos", 32, "Rio de Janeiro", 6200.00m);
    sampleData.Rows.Add(3, "Carlos Oliveira", 25, "Belo Horizonte", 4800.00m);
    sampleData.Rows.Add(4, "Ana Costa", 35, "Porto Alegre", 7100.00m);
    sampleData.Rows.Add(5, "Pedro Lima", 29, "Recife", 5300.00m);
    sampleData.Rows.Add(6, "Lucia Ferreira", 31, "Salvador", 5900.00m);
    sampleData.Rows.Add(7, "Roberto Alves", 27, "Fortaleza", 5100.00m);
    sampleData.Rows.Add(8, "Fernanda Rocha", 33, "Brasília", 6800.00m);

    // Bind ao DataGrid
    DataPreviewGrid.ItemsSource = sampleData.DefaultView;

    // Atualizar StatusBar
    StatusRowCount.Text = $"Rows: {sampleData.Rows.Count:N0}";
    StatusColumnCount.Text = $"Columns: {sampleData.Columns.Count}";
  }
}