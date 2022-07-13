using DesktopUI2.Models;

using ReactiveUI;
using System.Collections.Generic;
using System.Reactive;

namespace DesktopUI2.ViewModels
{
  public class SettingsPageViewModel : ReactiveObject, IRoutableViewModel
  {

    public IScreen HostScreen { get; }
    public string UrlPathSegment { get; } = "settings";

    public ReactiveCommand<Unit, Unit> GoBack => MainViewModel.RouterInstance.NavigateBack;

    private StreamViewModel _streamViewModel;

    public StreamState state { get; set; }

    public ProgressViewModel progress { get; set; }

    private List<SettingViewModel> _settings;
    public List<SettingViewModel> Settings
    {
      get => _settings;
      private set => this.RaiseAndSetIfChanged(ref _settings, value);
    }

    public SettingsPageViewModel(IScreen screen, List<SettingViewModel> settings, StreamViewModel streamViewModel)
    {
      HostScreen = screen;
      Settings = settings;
      _streamViewModel = streamViewModel;
    }

    public SettingsPageViewModel(IScreen screen, List<SettingViewModel> settings, StreamViewModel streamViewModel, StreamState localState, ProgressViewModel localProgress)
    {
      HostScreen = screen;
      Settings = settings;
      _streamViewModel = streamViewModel;
      state = localState;
      progress = localProgress;
    }
  }
}
