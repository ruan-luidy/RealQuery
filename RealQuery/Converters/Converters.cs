using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace RealQuery.Converters;

/// <summary>
/// Conversor para inverter booleano (usado para IsEnabled quando IsProcessing = true)
/// </summary>
public class InverseBooleanConverter : IValueConverter
{
  public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
  {
    if (value is bool boolValue)
      return !boolValue;

    return false;
  }

  public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
  {
    if (value is bool boolValue)
      return !boolValue;

    return false;
  }
}

/// <summary>
/// Conversor Boolean para Visibility
/// </summary>
public class BooleanToVisibilityConverter : IValueConverter
{
  public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
  {
    if (value is bool boolValue)
      return boolValue ? Visibility.Visible : Visibility.Collapsed;

    return Visibility.Collapsed;
  }

  public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
  {
    if (value is Visibility visibility)
      return visibility == Visibility.Visible;

    return false;
  }
}

/// <summary>
/// Conversor para mostrar/esconder baseado em objeto null/not null
/// </summary>
public class ObjectToVisibilityConverter : IValueConverter
{
  public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
  {
    if (value == null)
      return Visibility.Collapsed;

    // Para strings, verificar se não é vazio
    if (value is string str && string.IsNullOrWhiteSpace(str))
      return Visibility.Collapsed;

    // Para números, verificar se não é zero (opcional)
    if (value is int intValue && intValue == 0)
      return Visibility.Collapsed;

    if (value is TimeSpan timeSpan && timeSpan == TimeSpan.Zero)
      return Visibility.Collapsed;

    return Visibility.Visible;
  }

  public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
  {
    throw new NotImplementedException();
  }
}

/// <summary>
/// Conversor para formatar TimeSpan de forma amigável
/// </summary>
public class TimeSpanToStringConverter : IValueConverter
{
  public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
  {
    if (value is TimeSpan timeSpan)
    {
      if (timeSpan.TotalSeconds < 1)
        return $"{timeSpan.TotalMilliseconds:F0}ms";

      if (timeSpan.TotalMinutes < 1)
        return $"{timeSpan.TotalSeconds:F2}s";

      return timeSpan.ToString(@"mm\:ss\.fff");
    }

    return "-";
  }

  public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
  {
    throw new NotImplementedException();
  }
}

/// <summary>
/// Conversor para cores baseadas no status do step
/// </summary>
public class StepStatusToColorConverter : IValueConverter
{
  public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
  {
    if (value is bool isError && isError)
      return "#ff4757"; // Vermelho para erro

    if (parameter?.ToString() == "executed" && value is bool isExecuted && isExecuted)
      return "#2ed573"; // Verde para executado

    return "#ffa502"; // Laranja para pendente
  }

  public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
  {
    throw new NotImplementedException();
  }
}