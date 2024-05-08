using Speckle.Connectors.DUI.Models;
using Speckle.Connectors.DUI.Models.Card;
using Speckle.Connectors.Utils;
using Speckle.Newtonsoft.Json;

namespace Speckle.Connectors.Autocad.HostApp;

public class AutocadDocumentStore : DocumentModelStore
{
  private readonly string _nullDocumentName = "Null Doc";
  private string _previousDocName;
  private readonly AutocadDocumentManager _autocadDocumentManager;

  public AutocadDocumentStore(
    JsonSerializerSettings jsonSerializerSettings,
    AutocadDocumentManager autocadDocumentManager
  )
    : base(jsonSerializerSettings)
  {
    _autocadDocumentManager = autocadDocumentManager;
    _previousDocName = _nullDocumentName;

    // POC: Will be addressed to move it into AutocadContext!
    if (Application.DocumentManager.MdiActiveDocument != null)
    {
      IsDocumentInit = true;
      // POC: this logic might go when we have document management in context
      // It is with the case of if binding created with already a document
      // This is valid when user opens acad file directly double clicking
      OnDocChangeInternal(Application.DocumentManager.MdiActiveDocument);
    }

    // Whenever we switch document, it will be triggered for existing one
    Application.DocumentManager.DocumentToBeDeactivated += (_, _) => WriteToFile();
    Application.DocumentManager.DocumentToBeDestroyed += (_, _) => WriteToFile();
    Application.DocumentManager.DocumentActivated += (_, e) => OnDocChangeInternal(e.Document);

    // since below event triggered as secondary, it breaks the logic in OnDocChangeInternal function, leaving it here for now.
    // Autodesk.AutoCAD.ApplicationServices.Application.DocumentWindowCollection.DocumentWindowActivated += (_, args) =>
    //  OnDocChangeInternal((Document)args.DocumentWindow.Document);
  }

  /// <summary>
  /// Tracks whether the doc has been subscribed to save events.
  /// POC: two separate docs can have the same name, this is a brittle implementation - should be correlated with file location.
  /// </summary>
  private readonly List<string> _saveToDocSubTracker = new();

  private void OnDocChangeInternal(Document doc)
  {
    var currentDocName = doc != null ? doc.Name : _nullDocumentName;
    if (_previousDocName == currentDocName)
    {
      return;
    }

    _previousDocName = doc != null ? doc.Name : _nullDocumentName;

    if (doc != null && !_saveToDocSubTracker.Contains(doc.Name))
    {
      doc.BeginDocumentClose += (_, _) => WriteToFile();
      doc.Database.BeginSave += (_, _) => WriteToFile();
      _saveToDocSubTracker.Add(doc.Name);
    }

    ReadFromFile();
    OnDocumentChanged();
  }

  public override void ReadFromFile()
  {
    Models = new List<ModelCard>();

    // POC: Will be addressed to move it into AutocadContext!
    Document doc = Application.DocumentManager.MdiActiveDocument;

    if (doc == null)
    {
      return;
    }

    string? serializedModelCards = _autocadDocumentManager.ReadModelCards(doc);
    if (serializedModelCards == null)
    {
      return;
    }

    Models = Deserialize(serializedModelCards).NotNull();
  }

  public override void WriteToFile()
  {
    // POC: Will be addressed to move it into AutocadContext!
    Document doc = Application.DocumentManager.MdiActiveDocument;

    if (doc == null)
    {
      return;
    }

    string modelCardsString = Serialize();
    _autocadDocumentManager.WriteModelCards(doc, modelCardsString);
  }
}
