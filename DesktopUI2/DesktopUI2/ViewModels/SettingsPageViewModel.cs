using ReactiveUI;
using Splat;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Avalonia.Controls;
using DesktopUI2.Models.Settings;

namespace DesktopUI2.ViewModels
{
  public class SettingsPageViewModel : ReactiveObject
  {
    private List<SettingViewModel> _settings;
    public List<SettingViewModel> Settings
    {
      get => _settings;
      private set => this.RaiseAndSetIfChanged(ref _settings, value);
    }

    public SettingsPageViewModel(List<SettingViewModel> settings)
    {
      Settings = settings;
    }

    public void SaveCommand(Window window)
    {
      if (window != null)
        window.Close(true);
    }
  }
}
