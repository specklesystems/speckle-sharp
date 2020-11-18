using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Stylet;

namespace Speckle.DesktopUI.Utils
{
  public class ProgressReport : PropertyChangedBase
  {
    public int CurrentPercentage => Value / Maximum * 100;
    private int _value;

    public int Value
    {
      get => _value;
      set
      {
        SetAndNotify(ref _value, value);
        NotifyOfPropertyChange(nameof(CurrentPercentage));
      }
    }

    private int _maximum = 100;

    public int Maximum
    {
      get => _maximum;
      set => SetAndNotify(ref _maximum, value);
    }

    private int _progressDict;

    public int ProgressDict
    {
      get => _progressDict;
      set => SetAndNotify(ref _progressDict, value);
    }

    public async Task ResetProgress(int millisec = 4000)
    {
      await Task.Delay(millisec);
      Maximum = 100;
      Value = 0;
    }
  }

  public static class Link
  {
    public static void OpenInBrowser(string url)
    {
      if ( !RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ) return;

      url = url.Replace("&", "^&");
      Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") {CreateNoWindow = true});
    }
  }

  public static class Formatting
  {
    public static string TimeAgo(string timestamp)
    {
      TimeSpan timeAgo;
      try
      {
        timeAgo = DateTime.Now.Subtract(DateTime.Parse(timestamp));
      }
      catch ( FormatException e )
      {
        Debug.WriteLine("Could not parse the string to a DateTime");
        return "";
      }

      if ( timeAgo.TotalSeconds < 60 )
        return "less than a minute ago";
      if ( timeAgo.TotalMinutes < 60 )
        return $"about {timeAgo.Minutes} minute{PluralS(timeAgo.Minutes)} ago";
      if ( timeAgo.TotalHours < 24 )
        return $"about {timeAgo.Hours} hour{PluralS(timeAgo.Hours)} ago";
      if ( timeAgo.TotalDays < 7 )
        return $"about {timeAgo.Days} day{PluralS(timeAgo.Days)} ago";
      if ( timeAgo.TotalDays < 30 )
        return $"about {timeAgo.Days / 7} week{PluralS(timeAgo.Days / 7)} ago";
      if ( timeAgo.TotalDays < 365 )
        return $"about {timeAgo.Days / 30} month{PluralS(timeAgo.Days / 30)} ago";

      return $"over {timeAgo.Days / 356} year{PluralS(timeAgo.Days / 356)} ago";
    }

    public static string PluralS(int num)
    {
      return num != 1 ? "s" : "";
    }
  }
}
