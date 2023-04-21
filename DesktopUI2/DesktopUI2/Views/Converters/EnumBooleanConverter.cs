using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace DesktopUI2.Views.Converters;

public class EnumBooleanConverter : IValueConverter
{
  public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
  {
    return value?.ToString().Equals(parameter);
  }

  public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
  {
    return value?.Equals(true) == true ? Enum.Parse(targetType, parameter.ToString()) : BindingOperations.DoNothing;
  }
}
