using Avalonia.Controls;
using Avalonia.Controls.Selection;
using DesktopUI2.Views.Settings;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Windows.Input;

namespace DesktopUI2.Models.Settings
{
  public class MultiSelectBoxSetting : ReactiveObject, ISetting
  {
    public string Type => typeof(MultiSelectBoxSetting).ToString();
    public string Name { get; set; }
    public string Slug { get; set; }
    public string Icon { get; set; }
    public string Description { get; set; }
    public List<string> Values { get; set; }

    private string _selection;
    public string Selection
    {
      get
      {
        return string.Join(", ", Selections);
      }
      set => this.RaiseAndSetIfChanged(ref _selection, value);
    }

    public SelectionModel<string> SelectionModel { get; }
    public void SelectionChanged(object sender, SelectionModelSelectionChangedEventArgs e)
    {
      try
      {
        foreach (var sel in e.SelectedItems)
          if (!Selections.Contains(sel))
            Selections.Add(sel as string);
        foreach (var unsel in e.DeselectedItems)
          Selections.Remove(unsel as string);

        this.RaisePropertyChanged("Selection");
      }
      catch (Exception ex)
      {

      }
    }
    public ObservableCollection<string> Selections { get; set; } = new ObservableCollection<string>();
    public Type ViewType { get; } = typeof(MultiSelectBoxSettingView);
    public string Summary { get; set; }
    public MultiSelectBoxSetting()
    {
      SelectionModel = new SelectionModel<string>();
      SelectionModel.SingleSelect = false;
      SelectionModel.SelectionChanged += SelectionChanged;
    }

  }
}
