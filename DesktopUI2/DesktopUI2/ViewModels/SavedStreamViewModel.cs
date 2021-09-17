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

    public SavedStreamViewModel(StreamState streamState, IScreen hostScreen, ICommand removeSavedStreamCommand)

    {
      StreamState = streamState;

      HostScreen = hostScreen;
      RemoveSavedStreamCommand = removeSavedStreamCommand;

      //use dependency injection to get bindings
      Bindings = Locator.Current.GetService<ConnectorBindings>();

      GetStream().ConfigureAwait(false);
      GenerateMenuItems();
    }

    private void GenerateMenuItems()
    {
      var menu = new MenuItemViewModel { Header = new MaterialIcon { Kind = MaterialIconKind.EllipsisVertical, Foreground = Avalonia.Media.Brushes.Gray } };
      menu.Items = new List<MenuItemViewModel> {
        new MenuItemViewModel (EditSavedStreamCommand, "Edit",  MaterialIconKind.Cog),
        new MenuItemViewModel (ViewOnlineSavedStreamCommand, "View online",  MaterialIconKind.RocketLaunchOutline),
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
      var url = $"{StreamState.ServerUrl.TrimEnd('/')}/streams/{StreamState.StreamId}";
      //to open urls in .net core must set UseShellExecute = true
      Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });

    }

    public async void SendCommand()
    {
      Progress = new ProgressViewModel();
      await Bindings.SendStream(StreamState, Progress);
      Progress.Value = 0;

    }

    public async void ReceiveCommand()
    {
      Progress = new ProgressViewModel();
      await Bindings.ReceiveStream(StreamState, Progress);
      Progress.Value = 0;
    }

  }
}
