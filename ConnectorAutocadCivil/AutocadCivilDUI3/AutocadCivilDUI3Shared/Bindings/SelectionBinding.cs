using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using DUI3;
using DUI3.Bindings;
using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;

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
      Parent?.RunOnMainThread(OnSelectionChanged);
    };
    _visitedDocuments.Add(Application.DocumentManager.MdiActiveDocument);

    Application.DocumentManager.DocumentActivated += (sender, e) => OnDocumentChanged(e.Document);
  }

  private void OnDocumentChanged(Document document)
  {
    // TODO: null check here
    if (document == null)
    {
      return;
    }
    if (!_visitedDocuments.Contains(document))
    {
      document.ImpliedSelectionChanged += (_, _) =>
      {
        Parent.RunOnMainThread(OnSelectionChanged);
      };
      _visitedDocuments.Add(document);
    }
  }

  private void OnSelectionChanged()
  {
    SelectionInfo selInfo = GetSelection();
    Parent.SendToBrowser(DUI3.Bindings.SelectionBindingEvents.SetSelection, selInfo);
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
        using var tr = doc.TransactionManager.StartTransaction();
        foreach (SelectedObject obj in selection.Value)
        {
          var dbObject = tr.GetObject(obj.ObjectId, OpenMode.ForRead);
          if (dbObject == null /*|| !dbObject.Visible()*/ )
          {
            continue;
          }

          var handleString = dbObject.Handle.Value.ToString();
          objs.Add(handleString);
        }
        tr.Commit();
        tr.Dispose();
      }
    }
    return new SelectionInfo { SelectedObjectIds = objs, Summary = $"{objs.Count} objects" };
  }
}
