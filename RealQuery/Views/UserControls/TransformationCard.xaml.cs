using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using RealQuery.Core.Models;

namespace RealQuery.Views.UserControls;

/// <summary>
/// Card compacto (1 linha) para exibir um transformation step
/// </summary>
public partial class TransformationCard : UserControl
{
  #region Events

  public event EventHandler<TransformationStep>? StepClicked;
  public event EventHandler<TransformationStep>? DeleteRequested;

  #endregion

  public TransformationCard()
  {
    InitializeComponent();
    MouseLeftButtonUp += OnCardClick;
  }

  private void OnCardClick(object sender, MouseButtonEventArgs e)
  {
    if (DataContext is TransformationStep step)
    {
      StepClicked?.Invoke(this, step);
    }
  }

  protected override void OnKeyDown(KeyEventArgs e)
  {
    base.OnKeyDown(e);

    if (e.Key == Key.Delete && DataContext is TransformationStep step)
    {
      DeleteRequested?.Invoke(this, step);
      e.Handled = true;
    }
  }
}
