using Avalonia.Controls.Selection;
using DesktopUI2.Models;
using DesktopUI2.Models.Settings;
using ReactiveUI;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace DesktopUI2.ViewModels
{
  public class SettingViewModel : ReactiveObject
  {
    private ISetting _setting;


    public ISetting Setting
    {
      get => _setting;
      set
      {
        this.RaiseAndSetIfChanged(ref _setting, value);
        this.RaisePropertyChanged("Summary");
      }
    }

    private string _selection;
    public string Selection
    {
      get => _selection;
      set
      {
        //sets the selected item on the data model
        Setting.Selection = value;
        this.RaiseAndSetIfChanged(ref _selection, value);

      }
    }

    public StreamState State { get; set; }
    public ProgressViewModel Progress { get; set; }

    public SettingViewModel(ISetting setting)
    {
      Setting = setting;
      //restores the selected item
      Selection = setting.Selection;
    }
    public SettingViewModel(ISetting setting, StreamState localState, ProgressViewModel localProgress)
    {
      Setting = setting;
      //restores the selected item
      Selection = setting.Selection;
      Progress = localProgress;
      State = localState;
    }
  }
}
