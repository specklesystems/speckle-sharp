using ReactiveUI;
using Splat;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace DesktopUI2.ViewModels
{
  interface ICloseWindow
  {
    Action Close { get; set; }
  }

  public class SettingsPageViewModel : ReactiveObject
  {
    private SettingViewModel _selectedSetting;

    public event EventHandler SettingsSaved;

    public SettingViewModel SelectedSetting
    {
      get => _selectedSetting;
      set => this.RaiseAndSetIfChanged(ref _selectedSetting, value);
    }

    private List<SettingViewModel> _settings;
    public List<SettingViewModel> Settings
    {
      get => _settings;
      private set => this.RaiseAndSetIfChanged(ref _settings, value);
    }

    public SettingsPageViewModel(List<SettingViewModel> settings)
    {
      Settings = settings;
      SelectedSetting = Settings[0];
    }

    public void SaveCommand()
    {
      SettingsSaved?.Invoke(this, EventArgs.Empty);
    }
  }
}
