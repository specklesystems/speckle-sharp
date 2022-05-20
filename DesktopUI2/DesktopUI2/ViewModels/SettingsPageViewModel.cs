using Avalonia.Controls;
using ReactiveUI;
using System.Collections.Generic;
using System.Reactive;

namespace DesktopUI2.ViewModels
{
  public class SettingsPageViewModel : ReactiveObject, IRoutableViewModel
  {

    public IScreen HostScreen { get; }
    public string UrlPathSegment { get; } = "settings";

    public ReactiveCommand<Unit, Unit> GoBack => MainWindowViewModel.RouterInstance.NavigateBack;


    private List<SettingViewModel> _settings;
    public List<SettingViewModel> Settings
    {
      get => _settings;
      private set => this.RaiseAndSetIfChanged(ref _settings, value);
    }

    public SettingsPageViewModel(IScreen screen, List<SettingViewModel> settings)
    {
      HostScreen = screen;
      Settings = settings;
    }

    public void SaveCommand(Window window)
    {
      if (window != null)
        window.Close(true);
    }
  }
}
