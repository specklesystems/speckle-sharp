using Avalonia.Controls;
using Avalonia.Controls.Selection;
using DesktopUI2.Views.Settings;
using ReactiveUI;
using Speckle.Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Windows.Input;

namespace DesktopUI2.Models.Settings
{
  [JsonObject(MemberSerialization.OptIn)]
  public class MultiSelectBoxSetting : ReactiveObject, ISetting
  {
    [JsonProperty]
    public string Type => typeof(MultiSelectBoxSetting).ToString();
    [JsonProperty]
    public string Name { get; set; }
    [JsonProperty]
    public string Slug { get; set; }
    [JsonProperty]
    public string Icon { get; set; }
    [JsonProperty]
    public string Description { get; set; }

    [JsonProperty]
    public List<string> Values { get; set; }



    [JsonProperty]
    public string Selection { get; set; }


    //note this selection is not restored but it does not affect the functionality
    public SelectionModel<string> SelectionModel { get; }
    public void SelectionChanged(object sender, SelectionModelSelectionChangedEventArgs e)
    {
      Selection = string.Join(", ", SelectionModel.SelectedItems);
      this.RaisePropertyChanged("Selection");
    }

    public Type ViewType { get; } = typeof(MultiSelectBoxSettingView);
    [JsonProperty]
    public string Summary { get; set; }

    public MultiSelectBoxSetting()
    {
      SelectionModel = new SelectionModel<string>();
      SelectionModel.SingleSelect = false;
      SelectionModel.SelectionChanged += SelectionChanged;
    }


  }
}
