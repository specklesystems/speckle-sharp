using System.Collections.Generic;
using System.Diagnostics;
using AutocadCivilDUI3Shared.Utils;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using DUI3;
using DUI3.Bindings;

namespace AutocadCivilDUI3Shared.Bindings;

public class SelectionBinding : ISelectionBinding
{
  public string Name { get; set; } = "selectionBinding";
  public IBridge Parent { get; set; }

  private readonly List<Document> _visitedDocuments = new();

  public SelectionBinding()
  {
    Application.DocumentManager.MdiActiveDocument.ImpliedSelectionChanged += (_, _) =>
    {
      OnSelectionChanged();
    };
    _visitedDocuments.Add(Application.DocumentManager.MdiActiveDocument);

    Application.DocumentManager.DocumentActivated += (sender, e) => OnDocumentChanged(e.Document);
  }

  private void OnDocumentChanged(Document document)
  {
    if (!_visitedDocuments.Contains(document))
    {
      document.ImpliedSelectionChanged += (_, _) =>
      {
        OnSelectionChanged();
      };
      _visitedDocuments.Add(document);
    }
  }

  private void OnSelectionChanged()
  {
    Debug.WriteLine("Document: setSelection}");
    SelectionInfo selInfo = GetSelection();
    Parent?.SendToBrowser(DUI3.Bindings.SelectionBindingEvents.SetSelection, selInfo);
  }

  public SelectionInfo GetSelection()
  {
    Document doc = Application.DocumentManager.MdiActiveDocument;
    List<string> objs = new();
    if (doc != null)
    {
      PromptSelectionResult selection = doc.Editor.SelectImplied();
      if (selection.Status == PromptStatus.OK)
      {
        objs = selection.Value.GetHandles();
      }
    }
    return new SelectionInfo { SelectedObjectIds = objs, Summary = $"{objs.Count} objects" };
  }
}
