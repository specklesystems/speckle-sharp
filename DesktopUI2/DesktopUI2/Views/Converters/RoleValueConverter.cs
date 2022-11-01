using Avalonia.Data.Converters;
using System;

namespace DesktopUI2.Views.Converters
{

  public class RoleValueConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      if (value == null)
        return "public stream";
      return value.ToString().Replace("stream:", "");
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      if (value == null)
        return null;
      return "stream:" + value.ToString();
    }
  }
}

