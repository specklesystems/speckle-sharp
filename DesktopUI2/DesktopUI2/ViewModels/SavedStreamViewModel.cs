using DesktopUI2.Models;
using DynamicData;
using Material.Icons;
using Material.Icons.Avalonia;
using ReactiveUI;
using Speckle.Core.Api;
using Splat;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Text;
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

    private string Url { get => $"{StreamState.ServerUrl.TrimEnd('/')}/streams/{StreamState.StreamId}"; }

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
      }
      catch (Exception e)
      {
      }
    }

    public void EditSavedStreamCommand()
    {
      MainWindowViewModel.RouterInstance.Navigate.Execute(new StreamEditViewModel(HostScreen, StreamState));
    }

    public void ViewOnlineSavedStreamCommand()
    {
      //to open urls in .net core must set UseShellExecute = true
      Process.Start(new ProcessStartInfo(Url) { UseShellExecute = true });

    }

    public void CopyStreamURLCommand()
    {
      Avalonia.Application.Current.Clipboard.SetTextAsync(Url);

    }

    public async void SendCommand()
    {
      Progress = new ProgressViewModel();
      await Task.Run(() =>  Bindings.SendStream(StreamState, Progress));
      Progress.Value = 0;
      LastUsed = DateTime.Now.ToString();

    }

    public async void ReceiveCommand()
    {
      Progress = new ProgressViewModel();
      await Task.Run(() => Bindings.ReceiveStream(StreamState, Progress));
      Progress.Value = 0;
      LastUsed = DateTime.Now.ToString();
    }

  }
}
