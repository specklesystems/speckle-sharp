using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using Speckle.Core.Api;
using Stylet;

namespace Speckle.DesktopUI.Utils
{
  public class StringToUpperConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value != null && value is string)
      {
        return ((string)value).ToUpper();
      }

      return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      return null;
    }
  }

  public class SplitFirstNameConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value != null && value is string)
      {
        return ((string)value).Split(' ')[0];
      }

      return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }

  public class ListToStringConverter : IValueConverter
  {

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (targetType != typeof(string))
        throw new InvalidOperationException("The target must be a String");

      return string.Join(", ", ((BindableCollection<string>)value).ToArray());
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }

  public class TimeAgoConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      try
      {
        var timeAgo = Formatting.TimeAgo((string)value);
        return timeAgo;
      }
      catch (Exception e)
      {
        return value;
      }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      return null;
    }
  }

  public class NotNullToBooleanConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      return value != null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }

  public class NullAvatarToRobotConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value == null)
        return value;

      if (value.GetType() == typeof(User))
      {
        var user = (User)value;
        if (String.IsNullOrEmpty(user.avatar))
          return $"https://robohash.org/{user.id}";
        return user.avatar;
      }

      if (value.GetType() == typeof(Collaborator))
      {
        var collab = (Collaborator)value;
        if (String.IsNullOrEmpty(collab.avatar))
          return $"https://robohash.org/{collab.id}";
        return collab.avatar;
      }
      else
        throw new InvalidOperationException("Unrecognised type given to robot converter");
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }

  [ValueConversion(typeof(bool), typeof(bool))]
  public class InverseBooleanConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter,
      CultureInfo culture)
    {
      if (targetType != typeof(bool))
        throw new InvalidOperationException("The target must be a boolean");

      return !(bool)value;
    }

    public object ConvertBack(object value, Type targetType, object parameter,
      System.Globalization.CultureInfo culture)
    {
      throw new NotSupportedException();
    }
  }
}
