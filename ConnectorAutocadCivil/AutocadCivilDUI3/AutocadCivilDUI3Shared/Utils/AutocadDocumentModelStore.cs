using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices;
using DUI3.Models;
using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace AutocadCivilDUI3Shared.Utils;

public class AutocadDocumentModelStore : DocumentModelStore
{
  private static Document Doc { get; set; }
  private static string s_previousDocName;

  public AutocadDocumentModelStore()
  {
    if (Doc != null)
    {
      IsDocumentInit = true;
    }

    Application.DocumentManager.DocumentToBeDestroyed += (_, _) => WriteToFile();
    Application.DocumentManager.DocumentActivated += (_, e) => OnDocChangeInternal(e.Document);
    Autodesk.AutoCAD.ApplicationServices.Application.DocumentWindowCollection.DocumentWindowActivated += (_, args) =>
      OnDocChangeInternal(args.DocumentWindow.Document as Document);
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

    string serializedModelCards = AutocadDocumentManager.ReadModelCards(Doc);
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
    AutocadDocumentManager.WriteModelCards(Doc, modelCardsString);
  }
}
