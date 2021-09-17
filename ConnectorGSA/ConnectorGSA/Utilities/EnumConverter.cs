using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ConnectorGSA.Utilities
{
  public class EnumConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      var ParameterString = parameter as string;
      if (ParameterString == null)
      {
        return DependencyProperty.UnsetValue;
      }
      if (Enum.IsDefined(value.GetType(), value) == false)
      {
        return DependencyProperty.UnsetValue;
      }
      object paramvalue = Enum.Parse(value.GetType(), ParameterString);
      return paramvalue.Equals(value);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      var ParameterString = parameter as string;
      var valueAsBool = (bool)value;

      if (ParameterString == null || !valueAsBool)
      {
        try
        {
          return Enum.Parse(targetType, "0");
        }
        catch (Exception)
        {
          return DependencyProperty.UnsetValue;
        }
      }
      return Enum.Parse(targetType, ParameterString);
    }
  }
}
