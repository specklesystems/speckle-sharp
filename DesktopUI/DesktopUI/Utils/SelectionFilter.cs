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

    /// <summary>
    /// Used as the discriminator for deserialisation.
    /// </summary>
    string Type { get; }

    /// <summary>
    /// Shoud return a succint summary of the filter: what does it contain inside?
    /// </summary>
    string Summary { get; }

    /// <summary>
    /// Should contain a generic description of the filter and how it works.
    /// </summary>
    string Description { get; set; }

    /// <summary>
    /// Holds the values that the user selected from the filter. Not the actual objects.
    /// </summary>    
    List<string> Selection { get; set; }
  }

  public class ListSelectionFilter : ISelectionFilter
  {
    public string Type => typeof(ListSelectionFilter).ToString();

    public string Name { get; set; }
    public string Icon { get; set; }
    public string Description { get; set; }

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
    public string Type => typeof(ListSelectionFilter).ToString();

    public string Name { get; set; }
    public string Icon { get; set; }
    public string Description { get; set; }


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
    public string Name => Filter.Name;

    public ISelectionFilter Filter { get; }

    public object FilterView { get; private set; }

    private string _listItem;
    public string ListItem
    {
      get => _listItem;
      set
      {
        SetAndNotify(ref _listItem, value);
        if (value == null) return;
        if (ListItems.Contains(ListItem)) return;
        ListItems.Add(ListItem);
      }
    }

    public BindableCollection<string> ListItems { get; } = new BindableCollection<string>();

    public FilterTab(ISelectionFilter filter)
    {
      Filter = filter;

      switch (filter)
      {
        case PropertySelectionFilter f:
          FilterView = Activator.CreateInstance(Type.GetType($"Speckle.DesktopUI.Streams.Dialogs.FilterViews.ParameterFilterView"));
          break;
        case ListSelectionFilter f:
          FilterView = Activator.CreateInstance(Type.GetType($"Speckle.DesktopUI.Streams.Dialogs.FilterViews.CategoryFilterView"));
          break;
      }
    }

    public void RemoveListItem(string name)
    {
      ListItem = null;
      ListItems.Remove(name);
    }
  }
}
