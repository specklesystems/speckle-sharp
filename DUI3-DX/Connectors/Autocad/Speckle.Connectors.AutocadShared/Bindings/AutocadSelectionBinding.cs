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

    Application.DocumentManager.DocumentActivated += (sender, e) => OnDocumentChanged(e.Document);
  }

  private void OnDocumentChanged(Document document)
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
    // POC: Will be addressed to move it into AutocadContext!
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

          var handleString = dbObject.Handle.Value.ToString();
          objectTypes.Add(dbObject.GetType().Name);
          objs.Add(handleString);
        }

        tr.Commit();
        tr.Dispose();
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
