using Avalonia.Data.Converters;
using System;

namespace DesktopUI2.Views.Converters
{

  public class OpacityDoubleConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      double result = 0;
      double.TryParse(value.ToString(), out result);
      if (result > 0)
        return true;
      return false;
       
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      throw new NotSupportedException();
    }
  }
}

