using Autodesk.AutoCAD.DatabaseServices;
using Speckle.Connectors.DUI.Bindings;
using Speckle.Connectors.DUI.Bridge;
using Autodesk.AutoCAD.EditorInput;

namespace Speckle.Connectors.Autocad.Bindings;

public class AutocadSelectionBinding : ISelectionBinding
{
  private const string SELECTION_EVENT = "setSelection";

  private readonly List<Document> _visitedDocuments = new();

  public string Name { get; set; } = "selectionBinding";

  public IBridge Parent { get; }

  public AutocadSelectionBinding(IBridge parent)
  {
    Parent = parent;

    // POC: Use here Context for doc. In converters it's OK but we are still lacking to use context into bindings.
    // It is with the case of if binding created with already a document
    // This is valid when user opens acad file directly double clicking
    TryRegisterDocumentForSelection(Application.DocumentManager.MdiActiveDocument);
    Application.DocumentManager.DocumentActivated += (sender, e) => OnDocumentChanged(e.Document);
  }

  private void OnDocumentChanged(Document document) => TryRegisterDocumentForSelection(document);

  private void TryRegisterDocumentForSelection(Document document)
  {
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
    Parent?.Send(SELECTION_EVENT, selInfo);
  }

  public SelectionInfo GetSelection()
  {
    // POC: Will be addressed to move it into AutocadContext! https://spockle.atlassian.net/browse/CNX-9319
    Document doc = Application.DocumentManager.MdiActiveDocument;
    List<string> objs = new();
    List<string> objectTypes = new();
    if (doc != null)
    {
      PromptSelectionResult selection = doc.Editor.SelectImplied();
      if (selection.Status == PromptStatus.OK)
      {
        using var tr = doc.TransactionManager.StartTransaction();
        foreach (SelectedObject obj in selection.Value)
        {
          var dbObject = tr.GetObject(obj.ObjectId, OpenMode.ForRead);
          if (dbObject == null)
          {
            continue;
          }

          var handleString = dbObject.Handle.Value.ToString((IFormatProvider?)null);
          objectTypes.Add(dbObject.GetType().Name);
          objs.Add(handleString);
        }

        tr.Commit();
      }
    }
    List<string> flatObjectTypes = objectTypes.Select(o => o).Distinct().ToList();
    return new SelectionInfo
    {
      SelectedObjectIds = objs,
      Summary = $"{objs.Count} objects ({string.Join(", ", flatObjectTypes)})"
    };
  }
}
