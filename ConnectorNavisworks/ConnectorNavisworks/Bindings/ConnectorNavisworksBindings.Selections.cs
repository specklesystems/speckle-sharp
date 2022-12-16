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

          // The path for ModelItems is their node position at each level of the Models tree.
          // This is the de facto UID for that element within the file at that time.
          InwOaPath path = ComApiBridge.ToInwOaPath(descendant);

          // Neglect the Root Node
          if (((Array)path.ArrayData).ToArray<int>().Length == 0) continue;

          // Acknowledging that if a collection contains >=10000 children then this indexing will be inadequate
          string pointer = ((Array)path.ArrayData).ToArray<int>().Aggregate("",
            (current, value) => current + (value.ToString().PadLeft(4, '0') + "-")).TrimEnd('-');

          //var handle = path.nwHandle;

          selectedObjects.Add(pointer);
        }
      }

      // Converting this flat list of objects back to a tree will be handled in the Converter.
      var objects = selectedObjects.ToList();

      // Sorting here or in the converter doesn't really matter. Essentially the root nodes will be processed first.
      // Traversal of child nodes to include will be handled by cross reference with the list.
      objects.Sort((x, y) =>
        x.Length == y.Length ? string.Compare(x, y, StringComparison.Ordinal) : x.Length.CompareTo(y.Length));

      Cursor.Current = Cursors.Default;

      return objects;
    }
  }
}