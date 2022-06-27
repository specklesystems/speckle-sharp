using Avalonia.Controls;
using Avalonia.Data;
using DesktopUI2.ViewModels;
using DesktopUI2.Views.Windows.Dialogs;
using Material.Dialog;
using Material.Dialog.Icons;
using Material.Dialog.Interfaces;
using Speckle.Core.Api;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace DesktopUI2
{
  public static class Dialogs
  {
    public static async void ShowDialog(string title, string message, DialogIconKind icon)
    {
      Dialog d = new Dialog(title, message, icon);
      await d.ShowDialog();
    }

    public static IDialogWindow<DialogResult> SendReceiveDialog(string header, object dataContext)
    {
      return DialogHelper.CreateCustomDialog(new CustomDialogBuilderParams()
      {
        ContentHeader = header,
        WindowTitle = header,
        DialogHeaderIcon = Material.Dialog.Icons.DialogIconKind.Info,
        StartupLocation = WindowStartupLocation.CenterOwner,
        NegativeResult = new DialogResult("cancel"),
        Borderless = true,

        //Width = MainWindow.Instance.Width - 20,
        DialogButtons = new DialogButton[]
          {
            new DialogButton
            {
              Content = "CANCEL",
              Result = "cancel"
            },

          },
        Content = new ProgressBar()
        {
          Minimum = 0,
          DataContext = dataContext,
          [!ProgressBar.ValueProperty] = new Binding("Progress.Value"),
          [!ProgressBar.MaximumProperty] = new Binding("Progress.Max"),
          [!ProgressBar.IsIndeterminateProperty] = new Binding("Progress.IsIndeterminate")
        }
      });

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
        return "never";
      }

      if (timeAgo.TotalSeconds < 60)
        return "just now";
      if (timeAgo.TotalMinutes < 60)
        return $"{timeAgo.Minutes} minute{PluralS(timeAgo.Minutes)} ago";
      if (timeAgo.TotalHours < 24)
        return $"{timeAgo.Hours} hour{PluralS(timeAgo.Hours)} ago";
      if (timeAgo.TotalDays < 7)
        return $"{timeAgo.Days} day{PluralS(timeAgo.Days)} ago";
      if (timeAgo.TotalDays < 30)
        return $"{timeAgo.Days / 7} week{PluralS(timeAgo.Days / 7)} ago";
      if (timeAgo.TotalDays < 365)
        return $"{timeAgo.Days / 30} month{PluralS(timeAgo.Days / 30)} ago";

      return $"{timeAgo.Days / 356} year{PluralS(timeAgo.Days / 356)} ago";
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

  public static class ApiUtils
  {
    private static Dictionary<string, User> CachedUsers = new Dictionary<string, User>();
    private static Dictionary<string, AccountViewModel> CachedAccounts = new Dictionary<string, AccountViewModel>();

    public static void ClearCache()
    {
      CachedAccounts = new Dictionary<string, AccountViewModel>();
      CachedUsers = new Dictionary<string, User>();
    }

    private static async Task<User> GetUser(string userId, Client client)
    {
      if (CachedUsers.ContainsKey(userId))
        return CachedUsers[userId];

      User user = await client.UserGet(userId);

      if (user != null)
        CachedUsers[userId] = user;

      return user;
    }

    public static async Task<AccountViewModel> GetAccount(string userId, Client client)
    {
      if (CachedAccounts.ContainsKey(userId))
        return CachedAccounts[userId];

      User user = await GetUser(userId, client);

      if (user == null)
        return null;

      var avm = new AccountViewModel(user);
      CachedAccounts[userId] = avm;
      return avm;

    }
  }


  public static class Utils
  {
    public static bool IsValidEmail(string email)
    {
      string expression = "\\w+([-+.']\\w+)*@\\w+([-.]\\w+)*\\.\\w+([-.]\\w+)*";

      if (Regex.IsMatch(email, expression))
      {
        if (Regex.Replace(email, expression, string.Empty).Length == 0)
        {
          return true;
        }
      }
      return false;
    }
    public static Action Debounce(this Action func, int milliseconds = 300)
    {
      CancellationTokenSource cancelTokenSource = null;

      return () =>
      {
        cancelTokenSource?.Cancel();
        cancelTokenSource = new CancellationTokenSource();

        Task.Delay(milliseconds, cancelTokenSource.Token)
            .ContinueWith(t =>
            {
              if (t.IsCompleted)
              {
                func();
              }
            }, TaskScheduler.Default);
      };
    }
    public static Action<T> Debounce<T>(this Action<T> func, int milliseconds = 300)
    {
      CancellationTokenSource cancelTokenSource = null;

      return arg =>
      {
        cancelTokenSource?.Cancel();
        cancelTokenSource = new CancellationTokenSource();

        Task.Delay(milliseconds, cancelTokenSource.Token)
            .ContinueWith(t =>
            {
              if (t.IsCompleted)
              {
                func(arg);
              }
            }, TaskScheduler.Default);
      };
    }
  }
}
