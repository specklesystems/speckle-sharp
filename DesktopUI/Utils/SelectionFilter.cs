using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stylet;

namespace Speckle.DesktopUI.Utils
{
  public interface ISelectionFilter
  {
    string Name { get; set; }
    string Icon { get; set; }
    string Type { get; }
    List<string> Selection { get; set; }
  }

  public class ElementsSelectionFilter : ISelectionFilter
  {
    public string Name { get; set; }
    public string Icon { get; set; }

    public string Type
    {
      get { return typeof(ElementsSelectionFilter).ToString(); }
    }

    public List<string> Selection { get; set; } = new List<string>();
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
  }

  public class FilterTab
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

    private void LocateFilterView()
    {
      var viewName = $"Speckle.DesktopUI.Streams.Dialogs.FilterViews.{Filter.Name}FilterView";
      var type = Type.GetType(viewName);
      try
      {
        FilterView = Activator.CreateInstance(type);
      }
      catch ( Exception e )
      {
        Debug.WriteLine(e);
      }
    }
  }
}
