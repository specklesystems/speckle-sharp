using System;
using System.Collections.Generic;
using System.Diagnostics;
using Speckle.Core.Logging;
using Stylet;

namespace Speckle.DesktopUI.Utils
{
  public interface ISelectionFilter
  {
    string Name { get; set; }
    string Icon { get; set; }
    string Type { get; }

    List<string> Selection { get; set; }

    /// <summary>
    /// Shoud return a succint summary of the filter.
    /// </summary>
    string Summary { get; }
  }

  public class ListSelectionFilter : ISelectionFilter
  {
    public string Name { get; set; }
    public string Icon { get; set; }

    public string Type
    {
      get { return typeof(ListSelectionFilter).ToString(); }
    }

    public List<string> Values { get; set; }
    public List<string> Selection { get; set; } = new List<string>();

    public string Summary
    {
      get
      {
        if (Selection.Count != 0)
        {
          return string.Join(", ", Selection);
        } else
        {
          return "Not set.";
        }
      }
    }
  }

  public class PropertySelectionFilter : ISelectionFilter
  {
    public string Name { get; set; }
    public string Icon { get; set; }

    public string Type
    {
      get { return typeof(PropertySelectionFilter).ToString(); }
    }

    public List<string> Selection { get; set; } = new List<string>();

    public List<string> Values { get; set; }
    public List<string> Operators { get; set; }
    public string PropertyName { get; set; }
    public string PropertyValue { get; set; }
    public string PropertyOperator { get; set; }
    public bool HasCustomProperty { get; set; }

    public string Summary
    {
      get
      {
        return $"{PropertyName} {PropertyOperator} {PropertyValue}";
      }
    }
  }

  public class FilterTab : PropertyChangedBase
  {
    public string Name
    {
      get => Filter.Name;
    }

    public ISelectionFilter Filter { get; }

    public object FilterView { get; private set; }

    public FilterTab(ISelectionFilter filter)
    {
      Filter = filter;
      LocateFilterView();
    }

    private string _listItem;

    public string ListItem
    {
      get => _listItem;
      set
      {
        SetAndNotify(ref _listItem, value);
        if (ListItems.Contains(ListItem))return;
        ListItems.Add(ListItem);
      }
    }

    public BindableCollection<string> ListItems { get; } = new BindableCollection<string>();

    public void RemoveListItem(string name)
    {
      ListItems.Remove(name);
    }

    private void LocateFilterView()
    {
      var viewName = $"Speckle.DesktopUI.Streams.Dialogs.FilterViews.{Filter.Name}FilterView";
      var type = Type.GetType(viewName);
      try
      {
        FilterView = Activator.CreateInstance(type);
      }
      catch (Exception e)
      {
        Log.CaptureException(e);
      }
    }
  }
}
