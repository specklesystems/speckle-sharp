using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Speckle.DesktopUI.Utils
{
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
      return num > 1 ? "s" : "";
    }
  }
}
