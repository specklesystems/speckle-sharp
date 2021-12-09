using Avalonia.Controls;
using Avalonia.Controls.Selection;
using DesktopUI2.Models.Settings;
using DesktopUI2.Views.Settings;
using ReactiveUI;
using Splat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DesktopUI2.ViewModels
{
  public class SettingViewModel : ReactiveObject
  {
    private ConnectorBindings Bindings;

    private ISetting _setting;
    
    public ISetting Setting { get=> _setting;
      set
      {
        this.RaiseAndSetIfChanged(ref _setting, value);
        this.RaisePropertyChanged("Summary");
      } 
    }

    public UserControl SettingView { get; private set; }

    public SelectionModel<string> SelectionModel { get; }

    public string Summary { get { return Setting.Summary; } }


    public SettingViewModel(ISetting setting)
    {
      SelectionModel = new SelectionModel<string>();
      SelectionModel.SingleSelect = false;
      SelectionModel.SelectionChanged += SelectionChanged;

      //use dependency injection to get bindings
      Bindings = Locator.Current.GetService<ConnectorBindings>();

      Setting = setting;
      SettingView = setting.View;

      SettingView.DataContext = this;

    }

    #region DROPDOWN SETTING

    void SelectionChanged(object sender, SelectionModelSelectionChangedEventArgs e)
    {
      Setting.Selection = e.SelectedItems.First().ToString();

      this.RaisePropertyChanged("Summary");
    }
    private List<string> _valuesList { get; }

    #endregion

    public bool IsReady()
    {
      if (Setting is DropdownSetting && !Setting.Selection.Any())
        return false;

      return true;

    }

  }
}
