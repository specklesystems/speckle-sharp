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
    private ConnectorBindings Bindings;


    private List<SettingViewModel> _settings;
    public List<SettingViewModel> Settings
    {
      get => _settings;
      private set => this.RaiseAndSetIfChanged(ref _settings, value);
    }

    public SettingsPageViewModel(List<SettingViewModel> savedSettings)
    {
      //use dependency injection to get bindings
      Bindings = Locator.Current.GetService<ConnectorBindings>();

      //get available settings from our bindings
      Settings = new List<SettingViewModel>(Bindings.GetSettings().Select(x => new SettingViewModel(x)));


      if (savedSettings != null)
      {
        foreach (var savedSetting in savedSettings)
        {

          var s = Settings.FirstOrDefault(x => x.Setting.Slug == savedSetting.Setting.Slug);
          if (s != null)
          {
            if (s.Setting is ListBoxSetting lbs)
            {
              try
              {
                var i = lbs.Values.IndexOf(savedSetting.Setting.Selection);
                s.SelectionModel.Select(i);
                //s.Setting.Selection = savedSetting.Setting.Selection;
              }
              catch (Exception e)
              {

              }
            }


          }


        }
      }
    }

    public void SaveCommand(Window window)
    {
      if (window != null)
        window.Close(true);
    }
  }
}
