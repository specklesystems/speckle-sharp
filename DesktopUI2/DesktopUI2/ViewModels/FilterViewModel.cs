using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls.Selection;
using DesktopUI2.Models.Filters;
using ReactiveUI;
using Speckle.Core.Logging;
using Splat;

namespace DesktopUI2.ViewModels;

public class FilterViewModel : ReactiveObject
{
  private ISelectionFilter _filter;
  private ConnectorBindings Bindings;

  public FilterViewModel(ISelectionFilter filter)
  {
    try
    {
      SelectionModel = new SelectionModel<string>();
      SelectionModel.SingleSelect = false;
      SelectionModel.SelectionChanged += SelectionChanged;

      //use dependency injection to get bindings
      Bindings = Locator.Current.GetService<ConnectorBindings>();

      Filter = filter;

      //TODO should clean up this logic a bit
      //maybe have a model, view and viewmodel for each filter
      if (filter is ListSelectionFilter l)
        _valuesList = SearchResults = new List<string>(l.Values);

      //TODO:
      //FilterView.DataContext = this;

      if (filter is TreeSelectionFilter tree)
      {
        //TODO:
      }
    }
    catch (Exception ex)
    {
      SpeckleLog.Logger.Fatal(
        ex,
        "Failed to construct view model {viewModel} {exceptionMessage}",
        GetType(),
        ex.Message
      );
    }
  }

  public ISelectionFilter Filter
  {
    get => _filter;
    set
    {
      this.RaiseAndSetIfChanged(ref _filter, value);
      isSearching = true;
      RestoreSelectedItems();
      isSearching = false;
      this.RaisePropertyChanged(nameof(Summary));
    }
  }

  public SelectionModel<string> SelectionModel { get; }

  public string Summary => Filter.Summary;

  public bool IsReady()
  {
    if (Filter is ManualSelectionFilter && !Filter.Selection.Any())
      return false;

    if (Filter is ListSelectionFilter && !Filter.Selection.Any())
      return false;

    if (Filter is PropertySelectionFilter p)
      if (
        string.IsNullOrEmpty(p.PropertyName)
        || string.IsNullOrEmpty(p.PropertyOperator)
        || string.IsNullOrEmpty(p.PropertyValue)
      )
        return false;

    return true;
  }

  #region LIST FILTER

  private void SelectionChanged(object sender, SelectionModelSelectionChangedEventArgs e)
  {
    try
    {
      if (!isSearching)
      {
        foreach (var a in e.SelectedItems)
          if (!Filter.Selection.Contains(a))
            Filter.Selection.Add(a as string);
        foreach (var r in e.DeselectedItems)
          Filter.Selection.Remove(r as string);

        this.RaisePropertyChanged(nameof(Summary));
      }
    }
    catch (Exception ex)
    {
      SpeckleLog.Logger.Warning(
        ex,
        "Swallowing exception in {methodName}: {exceptionMessage}",
        nameof(SelectionChanged),
        ex.Message
      );
    }
  }

  private bool isSearching;
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

  // searching will change data source and remove selected items in the ListBox,
  // restore them as the query is cleared
  public void RestoreSelectedItems()
  {
    try
    {
      var itemsToRemove = new List<string>();

      if (Filter.Type == typeof(ListSelectionFilter).ToString())
      {
        foreach (var item in Filter.Selection)
        {
          if (!_valuesList.Contains(item))
            itemsToRemove.Add(item);
          if (!SelectionModel.SelectedItems.Contains(item))
            SelectionModel.Select(SearchResults.IndexOf(item));
        }

        foreach (var itemToRemove in itemsToRemove)
          Filter.Selection.Remove(itemToRemove);
      }

      this.RaisePropertyChanged(nameof(PropertyName));
      this.RaisePropertyChanged(nameof(PropertyValue));
      this.RaisePropertyChanged(nameof(PropertyOperator));
    }
    catch (Exception ex)
    {
      SpeckleLog.Logger.Warning(
        ex,
        "Swallowing exception in {methodName}: {exceptionMessage}",
        nameof(RestoreSelectedItems),
        ex.Message
      );
    }
  }

  public List<string> SearchResults { get; set; } = new();
  private List<string> _valuesList { get; } = new();

  #endregion

  #region MANUAL FILTER

  public void SetObjectSelection()
  {
    try
    {
      var objIds = Bindings.GetSelectedObjects();
      if (objIds == null || objIds.Count == 0)
        //Globals.Notify("Could not get object selection.");
        return;

      Filter.Selection = objIds;
      this.RaisePropertyChanged(nameof(Summary));
      //Globals.Notify("Object selection set.");
    }
    catch (Exception ex) { }
  }

  public void AddObjectSelection()
  {
    try
    {
      var objIds = Bindings.GetSelectedObjects();
      if (objIds == null || objIds.Count == 0)
        //Globals.Notify("Could not get object selection.");
        return;

      objIds.ForEach(id =>
      {
        if (Filter.Selection.FirstOrDefault(x => x == id) == null)
          Filter.Selection.Add(id);
      });
      this.RaisePropertyChanged(nameof(Summary));
      //Globals.Notify("Object added.");
    }
    catch (Exception ex) { }
  }

  public void RemoveObjectSelection()
  {
    try
    {
      var objIds = Bindings.GetSelectedObjects();
      if (objIds == null || objIds.Count == 0)
        //Globals.Notify("Could not get object selection.");
        return;

      var filtered = Filter.Selection.Where(o => objIds.IndexOf(o) == -1).ToList();

      if (filtered.Count == Filter.Selection.Count)
        //Globals.Notify("No objects removed.");
        return;

      //Globals.Notify($"{Filter.Selection.Count - filtered.Count} objects removed.");
      Filter.Selection = filtered;
      this.RaisePropertyChanged(nameof(Summary));
    }
    catch (Exception ex) { }
  }

  public void ClearObjectSelection()
  {
    Filter.Selection = new List<string>();
    this.RaisePropertyChanged(nameof(Summary));
    //Globals.Notify($"Selection cleared.");
  }

  #endregion

  #region PROPERTY FILTER

  //not the cleanest way, but it works
  //should create proper view models for each!
  public string PropertyName
  {
    get => (Filter as PropertySelectionFilter).PropertyName;
    set
    {
      (Filter as PropertySelectionFilter).PropertyName = value;
      this.RaisePropertyChanged(nameof(Summary));
    }
  }

  public string PropertyValue
  {
    get => (Filter as PropertySelectionFilter).PropertyValue;
    set
    {
      (Filter as PropertySelectionFilter).PropertyValue = value;
      this.RaisePropertyChanged(nameof(Summary));
    }
  }

  public string PropertyOperator
  {
    get => (Filter as PropertySelectionFilter).PropertyOperator;
    set
    {
      (Filter as PropertySelectionFilter).PropertyOperator = value;
      this.RaisePropertyChanged(nameof(Summary));
    }
  }

  #endregion
}
