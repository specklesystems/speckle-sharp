using Avalonia.Data;
using Avalonia.Data.Converters;
using System;

namespace DesktopUI2.Views.Converters
{
  public class EnumBooleanConverter : IValueConverter
  {

    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      return value?.ToString().Equals(parameter);
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {

      return value?.Equals(true) == true ? Enum.Parse(targetType, parameter.ToString()) : BindingOperations.DoNothing;
    }

  }
}
