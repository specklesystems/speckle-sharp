using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Autodesk.Navisworks.Api;
using Speckle.ConnectorNavisworks.Other;
using Application = Autodesk.Navisworks.Api.Application;
using Cursor = System.Windows.Forms.Cursor;

namespace Speckle.ConnectorNavisworks.Bindings;

public partial class ConnectorBindingsNavisworks
{
  /// <summary>
  ///   Parses list all selected Elements and their descendants that match criteria:
  ///   1. Is Selected
  ///   2. Is Visible in the current view
  /// </summary>
  /// <returns>List of unique pseudoIds</returns>
  public override List<string> GetSelectedObjects()
  {
    Cursor.Current = Cursors.WaitCursor;

    // Current document, models and selected elements.
    _doc = Application.ActiveDocument;

    IsFileAndModelsPresent();

    var appSelectedItems = _doc.CurrentSelection.SelectedItems;

    // Storing as a Set for consistency with the converter's handling of fragments and paths.
    var selectedObjects = new HashSet<string>();

    var visible = appSelectedItems.Where(IsElementVisible);
    var visibleItems = visible.ToList();
    var ids = visibleItems.Select(i => new Element().GetElement(i).PseudoId).Where(x => x != null);

    selectedObjects.UnionWith(ids);

    Cursor.Current = Cursors.Default;

    return selectedObjects.ToList();
  }

  /// <summary>
  ///   Checks is the Element is hidden or if any of its ancestors is hidden
  /// </summary>
  /// <param name="element"></param>
  /// <returns></returns>
  private static bool IsElementVisible(ModelItem element)
  {
    // Hidden status is stored at the earliest node in the hierarchy
    // All of the the tree path nodes need to not be Hidden
    return element.AncestorsAndSelf.All(x => x.IsHidden != true);
  }
}
