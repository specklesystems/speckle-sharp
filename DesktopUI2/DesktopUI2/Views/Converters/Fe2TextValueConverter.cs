using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace DesktopUI2.Views.Converters;

public class Fe2TextValueConverter : IValueConverter
{
  public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
  {
    var text = (string)parameter;
    //if usefe2
    if ((bool)value)
    {
      text = text.Replace("stream", "project");
      text = text.Replace("STREAM", "PROJECT");
      text = text.Replace("Stream", "Project");
      text = text.Replace("branch", "model");
      text = text.Replace("Branch", "Model");
      text = text.Replace("commit", "version");
      text = text.Replace("Commit", "Version");
    }
    return text;
  }

  public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
  {
    throw new NotSupportedException();
  }
}
