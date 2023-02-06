using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Autodesk.Navisworks.Api;
using Cursor = System.Windows.Forms.Cursor;
using Application = Autodesk.Navisworks.Api.Application;
using static Speckle.ConnectorNavisworks.Utils;

namespace Speckle.ConnectorNavisworks.Bindings
{
  public partial class ConnectorBindingsNavisworks
  {
    /// <summary>
    /// Parses list all selected Elements and their descendants that match criteria:
    /// 1. Is Selected
    /// 2. Is Visible in the current view 
    /// </summary>
    /// <returns>List of unique pseudoIds</returns>
    public override List<string> GetSelectedObjects()
    {
      Cursor.Current = Cursors.WaitCursor;

      // Current document, models and selected elements.
      Doc = Application.ActiveDocument;
      ModelItemCollection appSelectedItems = Doc.CurrentSelection.SelectedItems;

      // Storing as a Set for consistency with the converter's handling of fragments and paths.
      HashSet<string> selectedObjects = new HashSet<string>();

      var selectedItems = appSelectedItems;
      var visible = selectedItems.Where(IsElementVisible);
      //.SelectMany(x=>x.DescendantsAndSelf)
      var visibleItems = visible.ToList();
      var ids = visibleItems.Select(GetPseudoId).Where(x => x != null);

      selectedObjects.UnionWith(ids);

      Cursor.Current = Cursors.Default;

      if (visibleItems.Any() && selectedObjects.Count == 0)
      {
        // Handle Root Node Selection
      }



      return selectedObjects.ToList();
    }

    /// <summary>
    /// Checks is the Element is hidden or if any of its ancestors is hidden
    /// </summary>
    /// <param name="element"></param>
    /// <returns></returns>
    private static bool IsElementVisible(ModelItem element)
    {
      // Hidden status is stored at the earliest node in the hierarchy
      return element.AncestorsAndSelf.All(x => x.IsHidden != true);
    }
  }
}