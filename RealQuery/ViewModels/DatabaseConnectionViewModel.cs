using System.Data;
using System.Windows;
using Microsoft.Data.Sqlite;
using System.Data.SqlClient;
using RealQuery.Core.Models;
using RealQuery.Core.Services;

namespace RealQuery.ViewModels;

/// <summary>
/// ViewModel para o diálogo de conexão com banco de dados
/// </summary>
public partial class DatabaseConnectionViewModel : ObservableObject
{
  #region Observable Properties

  [ObservableProperty]
  private DatabaseType _selectedDatabaseType = DatabaseType.SqlServer;

  [ObservableProperty]
  private string _server = "localhost";

  [ObservableProperty]
  private string _database = "";

  [ObservableProperty]
  private string _username = "";

  [ObservableProperty]
  private string _password = "";

  [ObservableProperty]
  private string _sqliteFilePath = "";

  [ObservableProperty]
  private string _query = "SELECT * FROM ";

  [ObservableProperty]
  private string _connectionStatusMessage = "";

  [ObservableProperty]
  private bool _isTestingConnection;

  [ObservableProperty]
  private bool _connectionSuccessful;

  [ObservableProperty]
  private string _tableName = "";

  #endregion

  #region Public Properties

  /// <summary>
  /// Resultado da conexão para retornar ao chamador
  /// </summary>
  public DatabaseConnectionInfo? ConnectionInfo { get; private set; }

  /// <summary>
  /// Indica se o usuário confirmou o diálogo
  /// </summary>
  public bool DialogResult { get; set; }

  #endregion

  #region Commands

  [RelayCommand]
  private void BrowseSqliteFile()
  {
    var fileDialogService = new FileDialogService();
    var filePath = fileDialogService.OpenFileDialog(
      "Select SQLite Database",
      "SQLite Databases (*.db;*.sqlite;*.sqlite3)|*.db;*.sqlite;*.sqlite3|All Files (*.*)|*.*"
    );

    if (!string.IsNullOrEmpty(filePath))
    {
      SqliteFilePath = filePath;
    }
  }

  [RelayCommand]
  private async Task TestConnectionAsync()
  {
    try
    {
      IsTestingConnection = true;
      ConnectionStatusMessage = "Testing connection...";
      ConnectionSuccessful = false;

      var connectionString = BuildConnectionString();

      if (SelectedDatabaseType == DatabaseType.SqlServer)
      {
        await TestSqlServerConnectionAsync(connectionString);
      }
      else // SQLite
      {
        await TestSqliteConnectionAsync(connectionString);
      }

      ConnectionSuccessful = true;
      ConnectionStatusMessage = "✓ Connection successful!";
    }
    catch (Exception ex)
    {
      ConnectionSuccessful = false;
      ConnectionStatusMessage = $"✗ Connection failed: {ex.Message}";
    }
    finally
    {
      IsTestingConnection = false;
    }
  }

  [RelayCommand]
  private void ConfirmConnection()
  {
    if (string.IsNullOrWhiteSpace(Query))
    {
      MessageBox.Show(
        "Please enter a SQL query.",
        "Validation Error",
        MessageBoxButton.OK,
        MessageBoxImage.Warning
      );
      return;
    }

    if (SelectedDatabaseType == DatabaseType.SqlServer)
    {
      if (string.IsNullOrWhiteSpace(Server) || string.IsNullOrWhiteSpace(Database))
      {
        MessageBox.Show(
          "Please fill in Server and Database fields.",
          "Validation Error",
          MessageBoxButton.OK,
          MessageBoxImage.Warning
        );
        return;
      }
    }
    else // SQLite
    {
      if (string.IsNullOrWhiteSpace(SqliteFilePath))
      {
        MessageBox.Show(
          "Please select a SQLite database file.",
          "Validation Error",
          MessageBoxButton.OK,
          MessageBoxImage.Warning
        );
        return;
      }

      if (!File.Exists(SqliteFilePath))
      {
        MessageBox.Show(
          "The selected SQLite file does not exist.",
          "File Not Found",
          MessageBoxButton.OK,
          MessageBoxImage.Error
        );
        return;
      }
    }

    // Criar DatabaseConnectionInfo
    ConnectionInfo = new DatabaseConnectionInfo
    {
      Name = SelectedDatabaseType == DatabaseType.SqlServer
        ? $"{Server}/{Database}"
        : Path.GetFileName(SqliteFilePath),
      DatabaseType = SelectedDatabaseType,
      Server = Server,
      Database = Database,
      Username = Username,
      Password = Password,
      FilePath = SqliteFilePath,
      Query = Query,
      TableName = TableName,
      ConnectionString = BuildConnectionString()
    };

    DialogResult = true;
  }

  [RelayCommand]
  private void Cancel()
  {
    DialogResult = false;
  }

  #endregion

  #region Private Methods

  private string BuildConnectionString()
  {
    if (SelectedDatabaseType == DatabaseType.SqlServer)
    {
      if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
      {
        // Integrated Security
        return $"Server={Server};Database={Database};Integrated Security=true;TrustServerCertificate=true;";
      }
      else
      {
        // SQL Server Authentication
        return $"Server={Server};Database={Database};User Id={Username};Password={Password};TrustServerCertificate=true;";
      }
    }
    else // SQLite
    {
      return $"Data Source={SqliteFilePath}";
    }
  }

  private async Task TestSqlServerConnectionAsync(string connectionString)
  {
    using var connection = new SqlConnection(connectionString);
    await connection.OpenAsync();
    // Conexão bem-sucedida
  }

  private async Task TestSqliteConnectionAsync(string connectionString)
  {
    if (!File.Exists(SqliteFilePath))
    {
      throw new FileNotFoundException("SQLite database file not found.", SqliteFilePath);
    }

    using var connection = new SqliteConnection(connectionString);
    await connection.OpenAsync();
    // Conexão bem-sucedida
  }

  #endregion
}
