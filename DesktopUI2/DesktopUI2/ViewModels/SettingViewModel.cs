using DesktopUI2.Models.Settings;
using ReactiveUI;

namespace DesktopUI2.ViewModels;

public class SettingViewModel : ReactiveObject
{
  private string _selection;
  private ISetting _setting;

  public SettingViewModel(ISetting setting)
  {
    Setting = setting;
    //restores the selected item
    Selection = setting.Selection;
  }

  public ISetting Setting
  {
    get => _setting;
    set
    {
      this.RaiseAndSetIfChanged(ref _setting, value);
      this.RaisePropertyChanged("Summary");
    }
  }

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
}
