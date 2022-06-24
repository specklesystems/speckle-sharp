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
    private bool _popupVisible = false;
    public bool PopupVisible
    {
      get => _popupVisible;
      set
      {
        //sets the selected item on the data model
        _popupVisible = value;
        this.RaiseAndSetIfChanged(ref _popupVisible, value);

      }
    }
    //public ObservableCollection<string> Selections { get; set; }
    public ICommand TogglePopup { get; set; }
    public SettingViewModel()
    {
      Setting = new MultiSelectBoxSetting { Name = "Reference Point", Icon = "CrosshairsGps", Description = "Hello world. This is a setting.", 
        Values = new List<string>() { "Default", "Project Base Point", "Survey Point" }, Selections = new ObservableCollection<string>() { "Default", "bb", "Project Base Point", "Survey Point" } };
    }
    public SettingViewModel(ISetting setting)
    {
      Setting = setting;
      //restores the selected item
      Selection = setting.Selection;
      TogglePopup = ReactiveCommand.Create(() => PopupVisible = !PopupVisible);
    }


  }
}
