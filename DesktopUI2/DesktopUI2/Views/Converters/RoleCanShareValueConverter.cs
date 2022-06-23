using Avalonia.Data.Converters;
using System;

namespace DesktopUI2.Views.Converters
{

  public class RoleCanShareValueConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      return value != null && value.ToString() == "stream:owner";
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      throw new NotSupportedException();
    }
  }
}

