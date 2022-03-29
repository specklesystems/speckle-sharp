using Avalonia.Data.Converters;
using System;

namespace DesktopUI2.Views.Converters
{

  public class OpacityValueConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      if (value != null && ((bool)value))
        return 0.5;
      return 1;
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      throw new NotSupportedException();
    }
  }
}

