using System;
using System.Collections.Generic;
using System.Diagnostics;
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

    private List<Document> visitedDocuments = new List<Document>();

    public SelectionBinding()
    {
      Application.DocumentManager.MdiActiveDocument.ImpliedSelectionChanged += (_, _) => { OnSelectionChanged(); };
      visitedDocuments.Add(Application.DocumentManager.MdiActiveDocument);

      Application.DocumentManager.DocumentActivated += (sender, e) => OnDocumentChanged(e.Document);
    }

    private void OnDocumentChanged(Document document)
    {
      if (!visitedDocuments.Contains(document))
      {
        document.ImpliedSelectionChanged += (_, _) => { OnSelectionChanged(); };
        visitedDocuments.Add(document);
      }
    }

    private void OnSelectionChanged()
    {
      Debug.WriteLine("Document: setSelcetion}");
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
