using System;
using System.Collections.Generic;
using Avalonia.Controls.Selection;
using DesktopUI2.Views.Settings;
using ReactiveUI;
using Speckle.Newtonsoft.Json;

namespace DesktopUI2.Models.Settings;

[JsonObject(MemberSerialization.OptIn)]
public class MultiSelectBoxSetting : ReactiveObject, ISetting
{
  public MultiSelectBoxSetting()
  {
    SelectionModel = new SelectionModel<string>();
    SelectionModel.SingleSelect = false;
    SelectionModel.SelectionChanged += SelectionChanged;
  }

  [JsonProperty]
  public List<string> Values { get; set; }

  //note this selection is not restored but it does not affect the functionality
  public SelectionModel<string> SelectionModel { get; }

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
  public string Selection { get; set; }

  public Type ViewType { get; } = typeof(MultiSelectBoxSettingView);

  [JsonProperty]
  public string Summary { get; set; }

  public void SelectionChanged(object sender, SelectionModelSelectionChangedEventArgs e)
  {
    Selection = string.Join(", ", SelectionModel.SelectedItems);
    this.RaisePropertyChanged(nameof(Selection));
  }
}
