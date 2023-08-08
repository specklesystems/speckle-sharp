using System.Collections.Generic;
using AutocadCivilDUI3Shared.Utils;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using DUI3;
using DUI3.Bindings;

namespace AutocadCivilDUI3Shared.Bindings
{
  public class SelectionBinding : ISelectionBinding
  {
    public string Name { get; set; } = "selectionBinding";
    public IBridge Parent { get; set; }

    public SelectionBinding()
    {
      Application.DocumentManager.MdiActiveDocument.Editor.SelectionAdded += (_, _) => { OnSelectionChanged(); };
      Application.DocumentManager.MdiActiveDocument.Editor.SelectionRemoved += (_, _) => { OnSelectionChanged(); };
    }

    private void OnSelectionChanged()
    {
      var selInfo = GetSelection();
      Parent?.SendToBrowser(DUI3.Bindings.SelectionBindingEvents.SetSelection, selInfo);
    }

    public SelectionInfo GetSelection()
    {
      var doc = Application.DocumentManager.MdiActiveDocument;
      var objs = new List<string>();
      if (doc != null)
      {
        PromptSelectionResult selection = doc.Editor.SelectImplied();
        if (selection.Status == PromptStatus.OK)
          objs = selection.Value.GetHandles();
      }
      return new SelectionInfo
      {
        SelectedObjectIds = objs,
        Summary = $"{objs.Count} objects"
      };
    }
  }
}
