using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using ReactiveUI;
using Speckle.Core.Logging;

namespace DesktopUI2.ViewModels;

public class SettingsPageViewModel : ReactiveObject, IRoutableViewModel
{
  private List<SettingViewModel> _settings;

  private StreamViewModel _streamViewModel;

  public SettingsPageViewModel(IScreen screen, List<SettingViewModel> settings, StreamViewModel streamViewModel)
  {
    HostScreen = screen;
    Settings = settings;
    _streamViewModel = streamViewModel;
  }

  public ReactiveCommand<Unit, Unit> GoBack => MainViewModel.RouterInstance.NavigateBack;

  public List<SettingViewModel> Settings
  {
    get => _settings;
    private set => this.RaiseAndSetIfChanged(ref _settings, value);
  }

  public IScreen HostScreen { get; }
  public string UrlPathSegment { get; } = "settings";

  public void SaveCommand()
  {
    _streamViewModel.Settings = Settings.Select(x => x.Setting).ToList();

    MainViewModel.RouterInstance.NavigateBack.Execute();

    var settingData = new Dictionary<string, object> { { "name", "Settings Save" } };

    foreach (var setting in Settings)
    {
      try
      {
        settingData.Add(setting.Setting.Slug, setting.Setting.Selection);
      }
      catch { }
    }

    Analytics.TrackEvent(Analytics.Events.DUIAction, settingData);
  }
}
