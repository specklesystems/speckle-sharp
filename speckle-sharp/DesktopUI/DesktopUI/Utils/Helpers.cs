using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using Speckle.DesktopUI.Streams;
using Stylet;

namespace Speckle.DesktopUI.Utils
{
  public static class Globals
  {

    /// <summary>
    /// The root view model instance. Set by the root view model on init.
    /// </summary>
    public static RootViewModel RVMInstance { get; set; }

    /// <summary>
    /// The current host application's ui bindings. 
    /// </summary>
    public static ConnectorBindings HostBindings { get => RVMInstance.Bindings; }

    /// <summary>
    /// Stores a reference to the stream repository. It's set by the all streams view model on init.
    /// </summary>
    public static StreamsRepository Repo { get; set; }

    public static ResourceDictionary RootResourceDict { get; set; }

    /// <summary>
    /// Sends a notification to the main view's snack bar.
    /// </summary>
    /// <param name="message"></param>
    public static void Notify(string message)
    {
      RVMInstance.Notifications.Enqueue(message);
    }
  }

  public class ProgressReport : PropertyChangedBase
  {

    private ConcurrentDictionary<string, int> _ProgressDict;
    public ConcurrentDictionary<string, int> ProgressDict
    {
      get => _ProgressDict;
      set
      {
        ProgressSummary = "";
        foreach (var kvp in value)
        {
          ProgressSummary += $"{kvp.Key}: {kvp.Value} ";
        }
        //NOTE: progress set to indeterminate until the TotalChildrenCount is correct
        //ProgressSummary += $"Total: {Maximum}";
        _ProgressDict = value;
        NotifyOfPropertyChange(nameof(ProgressSummary));
      }
    }

    public string ProgressSummary { get; set; } = "";

    private int _value = 0;
    public int Value
    {
      get => _value;
      set
      {
        SetAndNotify(ref _value, value);
        NotifyOfPropertyChange(nameof(IsProgressing));
      }
    }

    public bool IsProgressing { get => Value != 0; }

    private int _maximum = 100;

    public int Maximum
    {
      get => _maximum;
      set => SetAndNotify(ref _maximum, value);
    }

    public void ResetProgress()
    {
      Maximum = 100;
      Value = 0;
    }

    public void Update(ConcurrentDictionary<string, int> pd)
    {
      ProgressDict = pd;
      Value = ProgressDict.Values.Last();
    }
  }

  public static class Link
  {
    public static void OpenInBrowser(string url)
    {
      if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;

      url = url.Replace("&", "^&"); // is this needed?
      Process.Start(url);
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
      catch (FormatException e)
      {
        Debug.WriteLine("Could not parse the string to a DateTime");
        return "";
      }

      if (timeAgo.TotalSeconds < 60)
        return "less than a minute ago";
      if (timeAgo.TotalMinutes < 60)
        return $"about {timeAgo.Minutes} minute{PluralS(timeAgo.Minutes)} ago";
      if (timeAgo.TotalHours < 24)
        return $"about {timeAgo.Hours} hour{PluralS(timeAgo.Hours)} ago";
      if (timeAgo.TotalDays < 7)
        return $"about {timeAgo.Days} day{PluralS(timeAgo.Days)} ago";
      if (timeAgo.TotalDays < 30)
        return $"about {timeAgo.Days / 7} week{PluralS(timeAgo.Days / 7)} ago";
      if (timeAgo.TotalDays < 365)
        return $"about {timeAgo.Days / 30} month{PluralS(timeAgo.Days / 30)} ago";

      return $"over {timeAgo.Days / 356} year{PluralS(timeAgo.Days / 356)} ago";
    }

    public static string PluralS(int num)
    {
      return num != 1 ? "s" : "";
    }

    public static string CommitInfo(string stream, string branch, string commitId)
    {
      string formatted = $"{stream}[ {branch} @ {commitId} ]";
      string clean = Regex.Replace(formatted, @"[^\u0000-\u007F]+", string.Empty).Trim(); // remove emojis and trim :( 
      return clean;
    }
  }

  //class ProgresBarAnimateBehavior : Behavior<ProgressBar>
  //{
  //  bool _IsAnimating = false;

  //  protected override void OnAttached()
  //  {
  //    base.OnAttached();
  //    ProgressBar progressBar = this.AssociatedObject;
  //    progressBar.ValueChanged += ProgressBar_ValueChanged;
  //  }

  //  private void ProgressBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
  //  {
  //    if (_IsAnimating)
  //      return;

  //    _IsAnimating = true;

  //    DoubleAnimation doubleAnimation = new DoubleAnimation
  //        (e.OldValue, e.NewValue, new Duration(TimeSpan.FromSeconds(0.3)), FillBehavior.Stop);
  //    doubleAnimation.Completed += Db_Completed;

  //    ((ProgressBar)sender).BeginAnimation(ProgressBar.ValueProperty, doubleAnimation);

  //    e.Handled = true;
  //  }

  //  private void Db_Completed(object sender, EventArgs e)
  //  {
  //    _IsAnimating = false;
  //  }

  //  protected override void OnDetaching()
  //  {
  //    base.OnDetaching();
  //    ProgressBar progressBar = this.AssociatedObject;
  //    progressBar.ValueChanged -= ProgressBar_ValueChanged;
  //  }
  //}

}
