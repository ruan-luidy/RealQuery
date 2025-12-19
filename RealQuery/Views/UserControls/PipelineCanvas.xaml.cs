using System.Windows.Controls;
using RealQuery.Core.Models;

namespace RealQuery.Views.UserControls;

/// <summary>
/// Canvas que exibe os transformation steps em sequência
/// </summary>
public partial class PipelineCanvas : UserControl
{
  #region Events

  public event EventHandler<TransformationStep>? StepSelected;
  public event EventHandler<TransformationStep>? StepDeleted;

  #endregion

  public PipelineCanvas()
  {
    InitializeComponent();
  }

  #region Event Handlers

  private void TransformationCard_StepClicked(object sender, TransformationStep e)
  {
    StepSelected?.Invoke(this, e);
  }

  private void TransformationCard_DeleteRequested(object sender, TransformationStep e)
  {
    StepDeleted?.Invoke(this, e);
  }

  #endregion
}
