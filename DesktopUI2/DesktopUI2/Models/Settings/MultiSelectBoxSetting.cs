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
    public ObservableCollection<string> SearchResults { get; } = new ObservableCollection<string>(CategoryNames);
    private List<string> _searchResults2 = new List<string> { "StructuralFraming", "Walls" };
    public List<string> SearchResults2
    {
      get => _searchResults2;
      set => this.RaiseAndSetIfChanged(ref _searchResults2, value);
    }
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
    public SelectionModel<string> SearchSelectionModel { get; }
    public void SelectionChanged(object sender, SelectionModelSelectionChangedEventArgs e)
    {
      try
      {
        if (!isSearching)
        {
          foreach (var sel in e.SelectedItems)
          {
            if (sel as string == "+Custom")
            {
              AddCustom = true;
            }
            else if (!Selections.Contains(sel))
              Selections.Add(sel as string);
          }
          foreach (var unsel in e.DeselectedItems)
          {
            if (unsel as string == "+Custom")
            {
              AddCustom = true;
            }
            Selections.Remove(unsel as string);
          }

          this.RaisePropertyChanged("Selection");
        }
      }
      catch (Exception ex)
      {

      }
    }
    public void SearchSelectionChanged(object sender, SelectionModelSelectionChangedEventArgs e)
    {
      //Values.Add("hellllo");
      //List<string> selections = new List<string> { "first one "};
      //foreach (var sel in e.SelectedItems)
      //{
      //  selections.Add(sel as string);
      //}
      //SearchResults2 = selections;

      ////this.RaisePropertyChanged("Values");
      //this.RaisePropertyChanged(nameof(SearchResults2));
      foreach (var sel in e.SelectedItems)
      {
        SearchResults2.Add(sel as string );
      }
      Test = string.Join(", ", SearchResults2);
      this.RaisePropertyChanged(nameof(SearchResults2));

      //foreach (var a in e.SelectedItems)
      //  if (!CustomSelections.Contains(a))
      //    CustomSelections.Add(a as string);
      //foreach (var r in e.DeselectedItems)
      //  CustomSelections.Remove(r as string);

      //SearchResults2 = new List<string> { "new", "list" };
      //SearchResults2.Add("more new");
      //this.RaisePropertyChanged(nameof(SearchResults2));
      ////Test = string.Join(", ", SearchResults2);

      //foreach (var item in CustomSelections)
      //{
      //  if (!SearchSelectionModel.SelectedItems.Contains(item))
      //    SearchSelectionModel.Select(SearchResults2.IndexOf(item));
      //}

      //if (!isSearching)
      //{
      //  foreach (var sel in e.SelectedItems)
      //  {
      //    Values.Add("hellllo");
      //    SearchResults2.Add("heyheyhey");
      //    Test = string.Join(", ", SearchResults2);
      //    this.RaisePropertyChanged("Values");
      //    this.RaisePropertyChanged("SearchResults2");
      //    // if the user selects a category, remove the other categories from the search results
      //    if (CategoryNames.Contains(sel as string))
      //    {
      //      SearchResults2.Add(sel as string);
      //      Test = "heyyyyy";
      //      CurrentCategoryName = sel as string;
      //      Values.Add(CurrentCategoryName);
      //      //SearchResults.Add("hey");
      //      this.RaisePropertyChanged("SearchResults");
      //    }
      //    else if (!CustomSelections.Contains(sel))
      //      CustomSelections.Add(sel as string);
      //  }
      //  foreach (var unsel in e.DeselectedItems)
      //  {
      //    // if the category is unselected, then reset the search values to make 
      //    // the user select another category
      //    if (CategoryNames.Contains(unsel as string))
      //    {
      //      SearchResults = new ObservableCollection<string>(CategoryNames);
      //      CustomSelections = new ObservableCollection<string>();
      //    }
      //    else
      //      CustomSelections.Remove(unsel as string);
      //  }
      //  this.RaisePropertyChanged("Selection");
      //}
    }
    public ObservableCollection<string> Selections { get; set; } = new ObservableCollection<string>();
    public ObservableCollection<string> CustomSelections { get; set; } = new ObservableCollection<string>();
    
    private bool _addCustom = false;
    public bool AddCustom
    {
      get => _addCustom;
      set => this.RaiseAndSetIfChanged(ref _addCustom, value);
    }
    //public ReactiveCommand<Unit, bool> TogglePopup { get; set; }
    //public ReactiveCommand<string, Unit> RemoveSelection { get; set; }
    private bool isSearching = false;
    private string _searchQuery;
    public string SearchQuery
    {
      get => _searchQuery;
      set
      {
        //isSearching = true;
        this.RaiseAndSetIfChanged(ref _searchQuery, value);
        SearchResults2 = new List<string> { "new", "list", "yo" };
        this.RaisePropertyChanged(nameof(SearchResults2));

        //SearchResults = new ObservableCollection<string>(_valuesList.Where(v => v.ToLower().Contains(SearchQuery.ToLower())).ToList());
        //this.RaisePropertyChanged(nameof(SearchResults));
        //isSearching = false;
        //RestoreSelectedItems();

      }
    }
    private List<string> _valuesList { get; } = new List<string>();
    // searching will change data source and remove selected items in the ListBox, 
    // restore them as the query is cleared
    public void RestoreSelectedItems()
    {
      try
      {
        var itemsToRemove = new List<string>();

        //foreach (var item in Filter.selection)
        //{
        //  if (!_valueslist.contains(item))
        //    itemstoremove.add(item);
        //  if (!selectionmodel.selecteditems.contains(item))
        //    selectionmodel.select(searchresults.indexof(item));
        //}

        //foreach (var itemtoremove in itemstoremove)
        //  filter.selection.remove(itemtoremove);

        //this.raisepropertychanged("propertyname");
        //this.raisepropertychanged("propertyvalue");
        //this.raisepropertychanged("propertyoperator");
      }
      catch (Exception ex)
      {

      }
    }
    public Type ViewType { get; } = typeof(MultiSelectBoxSettingView);
    public string Summary { get; set; }

    public MultiSelectBoxSetting()
    {
      SelectionModel = new SelectionModel<string>();
      SelectionModel.SingleSelect = false;
      SelectionModel.SelectionChanged += SelectionChanged;

      SearchSelectionModel = new SelectionModel<string>();
      SearchSelectionModel.SingleSelect = false;
      SearchSelectionModel.SelectionChanged += SearchSelectionChanged;
    }

  }
}
