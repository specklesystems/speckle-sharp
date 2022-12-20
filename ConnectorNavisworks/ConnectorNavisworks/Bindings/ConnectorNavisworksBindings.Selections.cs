using System.Collections.Generic;
using System.Linq;
using Autodesk.Navisworks.Api;
using Autodesk.Navisworks.Api.DocumentParts;
using Autodesk.Navisworks.Api.Interop.ComApi;
using ComApiBridge = Autodesk.Navisworks.Api.ComApi.ComApiBridge;
using System;
using Cursor = System.Windows.Forms.Cursor;
using System.Windows.Forms;
using Application = Autodesk.Navisworks.Api.Application;

namespace Speckle.ConnectorNavisworks.Bindings
{
  public partial class ConnectorBindingsNavisworks
  {
    private readonly object geometry = new object();

    public override List<string> GetSelectedObjects()
    {
      Cursor.Current = Cursors.WaitCursor;

      // Current document, models and selected elements.
      Doc = Application.ActiveDocument;
      DocumentModels documentModels = Doc.Models;
      ModelItemCollection appSelectedItems = Doc.CurrentSelection.SelectedItems;


      // Storing as a Set for consistency with the converter's handling of fragments and paths.
      // Not actually expecting a clash.
      HashSet<string> selectedObjects = new HashSet<string>();
      foreach (var modelItem in appSelectedItems)
      {
        // Descendants is a flattened list of all child Nodes beneath the selected ModelItem, no need for traversal.
        ModelItemEnumerableCollection descendants = modelItem.DescendantsAndSelf;

        foreach (ModelItem descendant in descendants)
        {
          // TODO: Advanced Setting to toggle inclusion of hidden elements within a selection.
          // When Navisworks hides items it hides the highest level Node rather than all child nodes.
          // A Model Item within a selection may be hidden only by virtue of the state of it's ancestor
          var hiddenAncestors = descendant.AncestorsAndSelf.Any(x => x.IsHidden == true);
          if (hiddenAncestors) continue;

          string pseudoId = GetPseudoId(descendant);
          if (pseudoId != null) // root node
          {
            selectedObjects.Add(pseudoId);
          }
        }
      }

      Cursor.Current = Cursors.Default;

      // Converting this flat list of objects back to a tree will be handled in the Converter.
      return selectedObjects.ToList();
    }
  }
}