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
    public ConnectorBindings Bindings { get; set; }
    public static List<string> CategoryNames { get; } = new List<string>{"StructuralFraming", "Walls"};
    public string CurrentCategoryName { get; set; } = string.Empty;
    public List<string> Values { get; set; }
    private bool isSearching = false;

    //public ObservableCollection<string> SearchResults { get; } = new ObservableCollection<string>(CategoryNames);
    //private List<string> _searchResults2 = new List<string> { "StructuralFraming", "Walls" };
    //public List<string> SearchResults2
    //{
    //  get => _searchResults2;
    //  set => this.RaiseAndSetIfChanged(ref _searchResults2, value);
    //}
    private string _selection;
    public string Selection
    {
      get
      {
        return string.Join(", ", Selections);
      }
      set => this.RaiseAndSetIfChanged(ref _selection, value);
    }
    private string _test = "test";
    public string Test
    {
      get
      {
        return _test;
      }
      set
      {
        this.RaiseAndSetIfChanged(ref _test, value);
      }
    }

    public SelectionModel<string> SelectionModel { get; }
    //public SelectionModel<string> SearchSelectionModel { get; }
    public void SelectionChanged(object sender, SelectionModelSelectionChangedEventArgs e)
    {
      try
      {
        if (!isSearching)
        {
          foreach (var sel in e.SelectedItems)
            if (!Selections.Contains(sel))
              Selections.Add(sel as string);
          foreach (var unsel in e.DeselectedItems)
            Selections.Remove(unsel as string);

          this.RaisePropertyChanged("Selection");
        }
      }
      catch (Exception ex)
      {

      }
    }
    //public void SearchSelectionChanged(object sender, SelectionModelSelectionChangedEventArgs e)
    //{
     
    //}
    public ObservableCollection<string> Selections { get; set; } = new ObservableCollection<string>();
    public ObservableCollection<string> CustomSelections { get; set; } = new ObservableCollection<string>();
    
    //private bool _addCustom = false;
    //public bool AddCustom
    //{
    //  get => _addCustom;
    //  set => this.RaiseAndSetIfChanged(ref _addCustom, value);
    //}

    public Type ViewType { get; } = typeof(MultiSelectBoxSettingView);
    public string Summary { get; set; }

    public MultiSelectBoxSetting()
    {
      SelectionModel = new SelectionModel<string>();
      SelectionModel.SingleSelect = false;
      SelectionModel.SelectionChanged += SelectionChanged;

      //SearchSelectionModel = new SelectionModel<string>();
      //SearchSelectionModel.SingleSelect = false;
      //SearchSelectionModel.SelectionChanged += SearchSelectionChanged;
    }

  }
}
