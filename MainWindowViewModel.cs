using MaterialDesignThemes.Wpf;
using Speckle.DesktopUI.Accounts;
using Speckle.DesktopUI.Feed;
using Speckle.DesktopUI.Inbox;
using Speckle.DesktopUI.Settings;
using Speckle.DesktopUI.Streams;
using Speckle.DesktopUI.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace Speckle.DesktopUI
{
  public class MainWindowViewModel : BindableBase
  {
    public MainWindowViewModel()
    {
      OpenLinkCommand = new RelayCommand<string>(OnOpenLink);
      CopyStreamCommand = new RelayCommand<string>(OnCopyStreamCommand);
      ToggleThemeCommand = new RelayCommand<bool>(OnToggleTheme);
      ViewItems = GetViewItems();
      SelectedItem = ViewItems[0];
      MessageQueue = new SnackbarMessageQueue();

      ITheme theme = new PaletteHelper().GetTheme();
      _darkMode = theme.Body == (Color)ColorConverter.ConvertFromString("#DD000000") ? false : true;
    }

    public RelayCommand<string> OpenLinkCommand { get; set; }
    public RelayCommand<string> CopyToClipboardCommand { get; set; }
    public RelayCommand<string> CopyStreamCommand { get; set; }
    public RelayCommand<bool> ToggleThemeCommand { get; set; }
    public ConnectorBindings Bindings { get; set; }

    private bool _darkMode;
    public bool DarkMode
    {
      get => _darkMode;
      set => SetProperty(ref _darkMode, value);
    }
    private ObservableCollection<ViewItem> _viewItems;
    public ObservableCollection<ViewItem> ViewItems
    {
      get => _viewItems;
      set => SetProperty(ref _viewItems, value);
    }

    private ViewItem _selectedItem;
    public ViewItem SelectedItem
    {
      get => _selectedItem;
      set => SetProperty(ref _selectedItem, value);
    }
    private int _selectedIndex;

    public int SelectedIndex
    {
      get => _selectedIndex;
      set => SetProperty(ref _selectedIndex, value);
    }

    private SnackbarMessageQueue _messageQueue;
    public SnackbarMessageQueue MessageQueue
    {
      get => _messageQueue;
      set => SetProperty(ref _messageQueue, value);
    }

    private ObservableCollection<ViewItem> GetViewItems()
    {
      return new ObservableCollection<ViewItem>
            {
                new ViewItem("Home", new StreamsHomeViewModel()),
                new ViewItem("Inbox", new InboxViewModel()),
                new ViewItem("Feed", new FeedViewModel()),
                new ViewItem("Settings", new SettingsViewModel())
            };
    }

    private void OnOpenLink(string url)
    {
      Link.OpenInBrowser(url);
    }

    private void OnCopyStreamCommand(string text)
    {
      if (text != null)
      {
        CopyAndSnackbar(text, "Stream ID copied to clipboard!");
      }
    }

    private void CopyAndSnackbar(string text, string message)
    {
      Clipboard.SetText(text);
      MessageQueue.Enqueue(message);
    }

    private void OnToggleTheme(bool darkmode)
    {
      PaletteHelper paletteHelper = new PaletteHelper();
      ITheme theme = paletteHelper.GetTheme();
      theme.SetBaseTheme(darkmode ? Theme.Dark : Theme.Light);
      DarkMode = darkmode;
    }
  }
}
