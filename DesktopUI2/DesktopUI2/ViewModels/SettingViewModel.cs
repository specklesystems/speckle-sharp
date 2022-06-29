using Avalonia.Controls.Selection;
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
    public ConnectorBindings Bindings;
  
    public SettingViewModel()
    {
      Bindings = new DummyBindings();
      Setting = new MultiSelectBoxSetting
      {
        Slug = "disallow-join",
        Name = "Disallow Join For Elements",
        Icon = "CallSplit",
        Description = "Determine which objects should not be allowed to join by default",
        Values = new List<string>() { "Architectural Walls", "Structural Walls", "Structural Framing" }
      };
    }
    public SettingViewModel(ISetting setting)
    {
      Setting = setting;
      //restores the selected item
      Selection = setting.Selection;
    }
  }
}
