using DesktopUI2.Models;
using DesktopUI2.Views;
using DesktopUI2.Views.Windows;
using DynamicData;
using Material.Icons;
using Material.Icons.Avalonia;
using ReactiveUI;
using Speckle.Core.Logging;
using Splat;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace DesktopUI2.ViewModels
{
  public class SavedStreamViewModel : StreamViewModelBase
  {
    public StreamState StreamState { get; set; }
    private IScreen HostScreen { get; }

    private ConnectorBindings Bindings;

    private List<MenuItemViewModel> _menuItems = new List<MenuItemViewModel>();

    public ICommand RemoveSavedStreamCommand { get; }
    public List<MenuItemViewModel> MenuItems
    {
      get => _menuItems;
      private set
      {
        this.RaiseAndSetIfChanged(ref _menuItems, value);
      }
    }

    public string LastUpdated
    {
      get
      {
        return "Updated " + Formatting.TimeAgo(StreamState.CachedStream.updatedAt);
      }
    }

    public string LastUsed
    {
      get
      {
        var verb = StreamState.IsReceiver ? "Received" : "Sent";
        if (StreamState.LastUsed == null)
          return "Never " + verb.ToLower();
        return $"{verb} {Formatting.TimeAgo(StreamState.LastUsed)}";
      }
      set
      {
        StreamState.LastUsed = value;
        this.RaisePropertyChanged("LastUsed");
      }
    }

    private string _notification;
    public string Notification
    {
      get => _notification;
      set
      {
        this.RaiseAndSetIfChanged(ref _notification, value);
        this.RaisePropertyChanged("ShowNotification");
      }
    }

    public bool ShowNotification
    {
      get => !string.IsNullOrEmpty(Notification);
    }

    private string Url { get => $"{StreamState.ServerUrl.TrimEnd('/')}/streams/{StreamState.StreamId}/branches/{StreamState.BranchName}"; }

    public SavedStreamViewModel(StreamState streamState, IScreen hostScreen, ICommand removeSavedStreamCommand)

    {
      StreamState = streamState;

      HostScreen = hostScreen;
      RemoveSavedStreamCommand = removeSavedStreamCommand;

      //use dependency injection to get bindings
      Bindings = Locator.Current.GetService<ConnectorBindings>();
      GetStream().ConfigureAwait(false);
      GenerateMenuItems();

      var updateTextTimer = new System.Timers.Timer();
      updateTextTimer.Elapsed += UpdateTextTimer_Elapsed;
      updateTextTimer.Interval = TimeSpan.FromMinutes(1).TotalMilliseconds;
      updateTextTimer.Enabled = true;
    }

    private void UpdateTextTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
    {
      this.RaisePropertyChanged("LastUsed");
      this.RaisePropertyChanged("LastUpdated");
    }

    private void GenerateMenuItems()
    {
      var menu = new MenuItemViewModel { Header = new MaterialIcon { Kind = MaterialIconKind.EllipsisVertical, Foreground = Avalonia.Media.Brushes.Gray } };
      menu.Items = new List<MenuItemViewModel> {
        new MenuItemViewModel (EditSavedStreamCommand, "Edit",  MaterialIconKind.Cog),
        new MenuItemViewModel (ViewOnlineSavedStreamCommand, "View online",  MaterialIconKind.ExternalLink),
        new MenuItemViewModel (CopyStreamURLCommand, "Copy URL to clipboard",  MaterialIconKind.ContentCopy),
        new MenuItemViewModel (OpenReportCommand, "Open Report",  MaterialIconKind.TextBox)
      };
      var customMenues = Bindings.GetCustomStreamMenuItems();
      if (customMenues != null)
        menu.Items.AddRange(customMenues.Select(x => new MenuItemViewModel(x, this.StreamState)).ToList());
      //remove is added last
      menu.Items.Add(new MenuItemViewModel(RemoveSavedStreamCommand, StreamState.Id, "Remove", MaterialIconKind.Bin));
      MenuItems.Add(menu);

      this.RaisePropertyChanged("MenuItems");
    }

    public async Task GetStream()
    {
      try
      {
        //use cached stream, then load a fresh one async 
        Stream = StreamState.CachedStream;
        Stream = await StreamState.Client.StreamGet(StreamState.StreamId);
        StreamState.CachedStream = Stream;

        //subscription
        StreamState.Client.SubscribeCommitCreated(StreamState.StreamId);
        StreamState.Client.OnCommitCreated += Client_OnCommitCreated;
      }
      catch (Exception e)
      {
      }
    }

    private async void Client_OnCommitCreated(object sender, Speckle.Core.Api.SubscriptionModels.CommitInfo info)
    {
      var branches = await StreamState.Client.StreamGetBranches(StreamState.StreamId);

      if (!StreamState.IsReceiver) return;

      var binfo = branches.FirstOrDefault(b => b.name == info.branchName);
      var cinfo = binfo.commits.items.FirstOrDefault(c => c.id == info.id);

      Notification = $"{cinfo.authorName} sent new data on branch {info.branchName}: {info.message}";
    }

    public void CloseNotificationCommand()
    {
      Notification = "";
    }

    public void EditSavedStreamCommand()
    {
      MainWindowViewModel.RouterInstance.Navigate.Execute(new StreamEditViewModel(HostScreen, StreamState));
      Tracker.TrackPageview("stream", "edit");
      Analytics.TrackEvent(Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Stream Edit" } });
    }

    public void ViewOnlineSavedStreamCommand()
    {
      //to open urls in .net core must set UseShellExecute = true
      Process.Start(new ProcessStartInfo(Url) { UseShellExecute = true });
      Tracker.TrackPageview(Tracker.STREAM_VIEW);
      Analytics.TrackEvent(Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Stream View" } });
    }

    public void CopyStreamURLCommand()
    {
      Avalonia.Application.Current.Clipboard.SetTextAsync(Url);
      Analytics.TrackEvent(Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Stream Copy Link" } });
    }

    public async void SendCommand()
    {
      Notification = "";
      Progress = new ProgressViewModel();
      Progress.IsProgressing = true;
      await Task.Run(() => Bindings.SendStream(StreamState, Progress));
      Progress.IsProgressing = false;

      if (!Progress.CancellationTokenSource.IsCancellationRequested)
      {
        LastUsed = DateTime.Now.ToString();
        Analytics.TrackEvent(StreamState.Client.Account, Analytics.Events.Send);
        Tracker.TrackPageview(Tracker.SEND);
      }

      if (Progress.Report.ConversionErrorsCount > 0 || Progress.Report.OperationErrorsCount > 0)
        Notification = "Something went wrong, please check the report.";
    }

    public async void ReceiveCommand()
    {
      Notification = "";
      Progress = new ProgressViewModel();
      Progress.IsProgressing = true;
      await Task.Run(() => Bindings.ReceiveStream(StreamState, Progress));
      Progress.IsProgressing = false;

      if (!Progress.CancellationTokenSource.IsCancellationRequested)
      {
        LastUsed = DateTime.Now.ToString();
        Analytics.TrackEvent(StreamState.Client.Account, Analytics.Events.Receive);
        Tracker.TrackPageview(Tracker.RECEIVE);
      }

      if (Progress.Report.ConversionErrorsCount > 0 || Progress.Report.OperationErrorsCount > 0)
        Notification = "Something went wrong, please check the report.";
    }

    public void CancelSendOrReceive()
    {
      Progress.CancellationTokenSource.Cancel();
      Analytics.TrackEvent(Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Cancel Send or Receive" } });
    }

    public void OpenReportCommand()
    {
      var report = new Report();
      report.Title = $"Report of the last operation, {LastUsed.ToLower()}";
      report.DataContext = Progress;
      report.ShowDialog(MainWindow.Instance);
      Analytics.TrackEvent(Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Open Report" } });
    }

  }
}
