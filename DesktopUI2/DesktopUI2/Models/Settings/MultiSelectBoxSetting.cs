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
    public SelectionModel<string> SelectionModel { get; set; }
    public void SelectionChanged(object sender, SelectionModelSelectionChangedEventArgs e)
    {
      try
      {
        //Selection = e.SelectedItems.ToString();
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
    public ObservableCollection<string> Selections { get; set; }
    private bool _popupVisible;
    public bool PopupVisible
    {
      get => _popupVisible;
      set => this.RaiseAndSetIfChanged(ref _popupVisible, value);
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
        isSearching = true;
        this.RaiseAndSetIfChanged(ref _searchQuery, value);

        SearchResults = new List<string>(_valuesList.Where(v => v.ToLower().Contains(SearchQuery.ToLower())).ToList());
        this.RaisePropertyChanged(nameof(SearchResults));
        isSearching = false;
        RestoreSelectedItems();

      }
    }
    public List<string> SearchResults { get; set; } = new List<string>();
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

  }
}
