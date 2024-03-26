using Speckle.Connectors.DUI.Models;
using Speckle.Connectors.DUI.Models.Card;
using Speckle.Newtonsoft.Json;

namespace Speckle.Connectors.Autocad.HostApp;

public class AutocadDocumentStore : DocumentModelStore
{
  private static Document Doc { get; set; }
  private static string s_previousDocName;
  private readonly AutocadDocumentManager _autocadDocumentManager;

  public AutocadDocumentStore(
    JsonSerializerSettings jsonSerializerSettings,
    AutocadDocumentManager autocadDocumentManager
  )
    : base(jsonSerializerSettings)
  {
    _autocadDocumentManager = autocadDocumentManager;
    if (Doc != null)
    {
      IsDocumentInit = true;
    }

    Application.DocumentManager.DocumentToBeDestroyed += (_, _) => WriteToFile();
    Application.DocumentManager.DocumentActivated += (_, e) => OnDocChangeInternal(e.Document);
    Autodesk.AutoCAD.ApplicationServices.Application.DocumentWindowCollection.DocumentWindowActivated += (_, args) =>
      OnDocChangeInternal((Document)args.DocumentWindow.Document);
  }

  /// <summary>
  /// Tracks whether the doc has been subscribed to save events.
  /// TODO: two separate docs can have the same name, this is a brittle implementation - should be correlated with file location.
  /// </summary>
  private readonly List<string> _saveToDocSubTracker = new();

  private void OnDocChangeInternal(Document doc)
  {
    Doc = doc;
    var nullDocName = "Null Doc";
    var currentDocName = doc != null ? doc.Name : nullDocName;
    if (s_previousDocName == currentDocName)
    {
      return;
    }

    s_previousDocName = doc != null ? doc.Name : nullDocName;

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
    if (Doc == null)
    {
      return;
    }

    string? serializedModelCards = _autocadDocumentManager.ReadModelCards(Doc);
    if (serializedModelCards == null)
    {
      return;
    }

    Models = Deserialize(serializedModelCards);
  }

  public override void WriteToFile()
  {
    if (Doc == null)
    {
      return;
    }

    string modelCardsString = Serialize();
    _autocadDocumentManager.WriteModelCards(Doc, modelCardsString);
  }
}
