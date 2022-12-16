using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
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
      var imgString = "";

      switch (value)
      {
        case null:
          return null;
        case User user:
          imgString = user.avatar ?? $"https://robohash.org/{user.id}";
          break;
        case Collaborator collab:
          imgString = collab.avatar ?? $"https://robohash.org/{collab.id}";
          break;
        case Account account:
          var client = new Client(account);
          LimitedUser userRes = new LimitedUser();
          try
          {
            userRes = Task.Run(async () => (await client.UserSearch(account.userInfo.email)).FirstOrDefault()).Result;
          }
          catch (Exception)
          {
            // server is offline
          }
          imgString = userRes.avatar ?? $"https://robohash.org/{account.userInfo.id}";
          break;
        default:
          throw new InvalidOperationException("Unrecognised type given to robot converter");
      }

      if (imgString.StartsWith("http")) return imgString;
      return StringToBitmap(imgString);
    }

    private BitmapImage StringToBitmap(string value)
    {
      if (!value.StartsWith("data:image/")) return null;
      var str = value.Split(',')[1];
      var img = new BitmapImage();
      img.BeginInit();
      img.StreamSource = new MemoryStream(System.Convert.FromBase64String(str));
      img.EndInit();

      return img;
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
