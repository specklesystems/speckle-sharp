using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using DesktopUI2.ViewModels;
using DesktopUI2.ViewModels.MappingTool;
using DesktopUI2.Views;
using DesktopUI2.Views.Windows.Dialogs;
using Material.Dialog;
using Material.Dialog.Icons;
using Material.Dialog.Interfaces;
using SkiaSharp;
using Speckle.Core.Api;
using Speckle.Core.Api.GraphQL.Models;
using Speckle.Core.Credentials;
using Speckle.Core.Helpers;
using Speckle.Core.Logging;

namespace DesktopUI2;

public static class Dialogs
{
  public static async void ShowDialog(string title, string message, DialogIconKind icon)
  {
    Dialog d = new(title, message, icon);
    await d.ShowDialog().ConfigureAwait(true);
  }

  public static async void ShowMapperDialog(string title, string message, DialogIconKind icon)
  {
    Dialog d = new(title, message, icon);
    await d.ShowDialog(MappingsViewModel.Instance).ConfigureAwait(true);
  }

  public static IDialogWindow<DialogResult> SendReceiveDialog(string header, object dataContext)
  {
    return DialogHelper.CreateCustomDialog(
      new CustomDialogBuilderParams
      {
        ContentHeader = header,
        WindowTitle = header,
        DialogHeaderIcon = DialogIconKind.Info,
        StartupLocation = WindowStartupLocation.CenterOwner,
        NegativeResult = new DialogResult("cancel"),
        Borderless = true,
        //Width = MainWindow.Instance.Width - 20,
        DialogButtons = new DialogButton[]
        {
          new() { Content = "CANCEL", Result = "cancel" }
        },
        Content = new ProgressBar
        {
          Minimum = 0,
          DataContext = dataContext,
          [!RangeBase.ValueProperty] = new Binding("Progress.Value"),
          [!RangeBase.MaximumProperty] = new Binding("Progress.Max"),
          [!ProgressBar.IsIndeterminateProperty] = new Binding("Progress.IsIndeterminate")
        }
      }
    );
  }
}

public static class Formatting
{
  /// <inheritdoc cref="Helpers.TimeAgo(DateTime, string)"/>
  [DebuggerStepThrough]
  public static string TimeAgo(DateTime? timestamp)
  {
    return Helpers.TimeAgo(timestamp);
  }

  /// <inheritdoc cref="Helpers.TimeAgo(DateTime)"/>
  [DebuggerStepThrough]
  public static string TimeAgo(DateTime timestamp)
  {
    return Helpers.TimeAgo(timestamp);
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
  private static Dictionary<string, UserBase> CachedUsers = new();
  private static Dictionary<string, AccountViewModel> CachedAccounts = new();

  public static void ClearCache()
  {
    CachedAccounts = new Dictionary<string, AccountViewModel>();
    CachedUsers = new Dictionary<string, UserBase>();
  }

  private static async Task<UserBase> GetUser(string userId, Client client)
  {
    if (CachedUsers.ContainsKey(userId))
    {
      return CachedUsers[userId];
    }

    var user = await client.OtherUserGet(userId).ConfigureAwait(true);

    if (user != null)
    {
      CachedUsers[userId] = user;
    }

    return user;
  }

  public static async Task<AccountViewModel> GetAccount(string userId, Client client)
  {
    if (CachedAccounts.ContainsKey(userId))
    {
      return CachedAccounts[userId];
    }

    var user = await GetUser(userId, client).ConfigureAwait(true);

    if (user == null)
    {
      return null;
    }

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
        .ContinueWith(
          t =>
          {
            if (t.IsCompleted)
            {
              func();
            }
          },
          TaskScheduler.Default
        );
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
        .ContinueWith(
          t =>
          {
            if (t.IsCompleted)
            {
              func(arg);
            }
          },
          TaskScheduler.Default
        );
    };
  }

  public static void LaunchManager()
  {
    string path = "";
    try
    {
      Analytics.TrackEvent(Analytics.Events.DUIAction, new Dictionary<string, object> { { "name", "Launch Manager" } });

      if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
      {
        path = @"/Applications/Manager for Speckle.app";
      }

      if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
      {
        path = Path.Combine(
          Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
          "Speckle",
          "Manager",
          "Manager.exe"
        );
      }

      if (File.Exists(path) || Directory.Exists(path))
      {
        Open.File(path);
      }
      else
      {
        Open.Url("https://speckle.systems/download");
      }
    }
    catch (Exception ex)
    {
      SpeckleLog.Logger.ForContext("path", path).Error(ex, "Failed to launch Manager");
    }
  }

  public static async Task AddAccountCommand()
  {
    try
    {
      //IsLoggingIn = true;


      var dialog = new AddAccountDialog(AccountManager.GetDefaultServerUrl());
      var result = await dialog.ShowDialog<string>().ConfigureAwait(true);

      if (result != null)
      {
        Uri u;
        if (!Uri.TryCreate(result, UriKind.Absolute, out u))
        {
          Dialogs.ShowDialog("Error", "Invalid URL", DialogIconKind.Error);
        }
        else
        {
          Analytics.TrackEvent(
            Analytics.Events.DUIAction,
            new Dictionary<string, object> { { "name", "Account Add" } }
          );
          try
          {
            await AccountManager.AddAccount(result).ConfigureAwait(true);
            await Task.Delay(1000).ConfigureAwait(true);

            MainViewModel.Instance.NavigateToDefaultScreen();
          }
          catch (Exception ex)
          {
            SpeckleLog.Logger
              .ForContext("dialogResult", result)
              .Warning(
                ex,
                "Swallowing exception in {methodName} {exceptionMessage}",
                nameof(AddAccountCommand),
                nameof(AddAccountCommand),
                ex.Message
              );

            //errors already handled in AddAccount

            MainUserControl.NotificationManager.Show(
              new PopUpNotificationViewModel
              {
                Title = "Something went wrong...",
                Message = ex.Message,
                Expiration = TimeSpan.Zero,
                Type = NotificationType.Error
              }
            );
          }
        }
      }

      //IsLoggingIn = false;
    }
    catch (Exception ex)
    {
      SpeckleLog.Logger.Fatal(ex, "Failed to add account {viewModel} {exceptionMessage}", ex.Message);
    }
  }

  public static byte[] ResizeImage(
    byte[] fileContents,
    int maxWidth,
    int maxHeight,
    SKFilterQuality quality = SKFilterQuality.Medium
  )
  {
    using MemoryStream ms = new(fileContents);
    using SKBitmap sourceBitmap = SKBitmap.Decode(ms);

    int height = Math.Min(maxHeight, sourceBitmap.Height);
    int width = Math.Min(maxWidth, sourceBitmap.Width);

    using SKBitmap scaledBitmap = sourceBitmap.Resize(new SKImageInfo(width, height), quality);
    using SKImage scaledImage = SKImage.FromBitmap(scaledBitmap);
    using SKData data = scaledImage.Encode();

    return data.ToArray();
  }
}
