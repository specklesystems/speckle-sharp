using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Autodesk.Navisworks.Api;
using Speckle.ConnectorNavisworks.Other;
using Cursor = System.Windows.Forms.Cursor;

namespace Speckle.ConnectorNavisworks.Bindings;

public partial class ConnectorBindingsNavisworks
{
  /// <inheritdoc />
  /// <summary>
  ///   Parses list all selected Elements and their descendants that match criteria:
  ///   1. Is Selected
  ///   2. Is Visible in the current view
  /// </summary>
  /// <returns>List of unique IndexPaths as strings</returns>
  public override List<string> GetSelectedObjects()
  {
    Cursor.Current = Cursors.WaitCursor;

    IsFileAndAreModelsPresent();

    // Storing as a Set for consistency with the converter's handling of fragments and paths.
    var selectedObjects = new HashSet<string>(
      s_activeDoc.CurrentSelection.SelectedItems.Where(IsElementVisible).Select(Element.ResolveModelItemToIndexPath)
    );

    Cursor.Current = Cursors.Default;

    return selectedObjects.ToList();
  }

  /// <summary>
  ///   Checks is the Element is hidden or if any of its ancestors is hidden
  /// </summary>
  /// <param name="element"></param>
  /// <returns></returns>
  private static bool IsElementVisible(ModelItem element) =>
    // Hidden status is stored at the earliest node in the hierarchy
    // All the tree path nodes need to not be Hidden
    element.AncestorsAndSelf.All(x => x.IsHidden != true);
}
