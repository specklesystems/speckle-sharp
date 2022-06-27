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
  
    public SettingViewModel()
    {
      Setting = new MultiSelectBoxSetting
      {
        Name = "Disallow Join For Elements",
        Icon = "CrosshairsGps",
        Description = "Hello world. This is a setting.",
        Values = new List<string>() { "Beams", "Columns", "Walls" },
        Selections = new ObservableCollection<string>() { "Beams", "Columns", "Walls" }
      };
    }
    public SettingViewModel(ISetting setting)
    {
      Setting = setting;
      //restores the selected item
      Selection = setting.Selection;
      if (Setting is MultiSelectBoxSetting)
      {
        var MultiSelectBox = (MultiSelectBoxSetting)Setting;
        MultiSelectBox.TogglePopup = ReactiveCommand.Create(() => MultiSelectBox.PopupVisible = !MultiSelectBox.PopupVisible);
        MultiSelectBox.RemoveSelection = ReactiveCommand.Create<string>(sel => MultiSelectBox.Selections.Remove(sel));
      }
      //else
      //{
      //  TogglePopup = ReactiveCommand.Create(());
      //}
    }


  }
}
