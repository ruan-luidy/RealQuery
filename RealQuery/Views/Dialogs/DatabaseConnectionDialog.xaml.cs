using System.Windows;
using System.Windows.Controls;
using RealQuery.ViewModels;

namespace RealQuery.Views.Dialogs;

/// <summary>
/// Interaction logic for DatabaseConnectionDialog.xaml
/// </summary>
public partial class DatabaseConnectionDialog
{
  public DatabaseConnectionViewModel ViewModel { get; }

  public DatabaseConnectionDialog()
  {
    InitializeComponent();
    ViewModel = new DatabaseConnectionViewModel();
    DataContext = ViewModel;

    // Configure commands to close dialog
    ViewModel.ConfirmConnectionCommand.Execute(null);
    ViewModel.CancelCommand.Execute(null);

    // Override command handlers to close window
    ViewModel.PropertyChanged += (s, e) =>
    {
      if (e.PropertyName == nameof(ViewModel.DialogResult))
      {
        DialogResult = ViewModel.DialogResult;
        Close();
      }
    };
  }

  private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
  {
    if (sender is PasswordBox passwordBox)
    {
      ViewModel.Password = passwordBox.Password;
    }
  }

  /// <summary>
  /// Shows the dialog and returns the connection info if user confirms
  /// </summary>
  public static DatabaseConnectionViewModel? ShowDialog(Window owner)
  {
    var dialog = new DatabaseConnectionDialog
    {
      Owner = owner
    };

    var result = dialog.ShowDialog();

    if (result == true && dialog.ViewModel.ConnectionInfo != null)
    {
      return dialog.ViewModel;
    }

    return null;
  }
}
