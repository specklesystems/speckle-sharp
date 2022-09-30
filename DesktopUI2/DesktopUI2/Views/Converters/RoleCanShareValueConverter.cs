using Avalonia.Data.Converters;
using System;

namespace DesktopUI2.Views.Converters
{

  public class RoleCanShareValueConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      var canShare = value != null && value.ToString() == "stream:owner";
      var negate = parameter != null && parameter.ToString() == "not";
      return negate ? !canShare : canShare;
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      throw new NotSupportedException();
    }
  }
}

