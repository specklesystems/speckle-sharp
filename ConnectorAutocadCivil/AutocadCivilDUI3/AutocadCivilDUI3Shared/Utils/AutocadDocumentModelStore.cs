using System;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using DUI3.Models;

namespace AutocadCivilDUI3Shared.Utils
{
  public class AutocadDocumentModelStore : DocumentModelStore
  {
    private static Document Doc => Application.DocumentManager.MdiActiveDocument;
    private static string _previousDocName;

    public AutocadDocumentModelStore()
    {
      if (Doc != null)
      {
        IsDocumentInit = true;
      }
      Application.DocumentManager.MdiActiveDocument.BeginDocumentClose += (_, _) => WriteToFile();
      Application.DocumentManager.MdiActiveDocument.Editor.Document.Database.BeginSave += (_, _) => WriteToFile();
      Application.DocumentWindowCollection.DocumentWindowActivated += (sender, e) => NotifyDocumentChangedIfNeeded(e.DocumentWindow.Document as Document);
      Application.DocumentManager.DocumentActivated += (sender, e) => NotifyDocumentChangedIfNeeded(e.Document);
    }

    private void NotifyDocumentChangedIfNeeded(Document doc)
    {
      if (doc == null || Doc == null) return;
      if (_previousDocName == doc.Name) return;

      _previousDocName = doc.Name;
      ReadFromFile();
      OnDocumentChanged();
    }

    public override void ReadFromFile()
    {
      var serializedModelCards = AutocadDocumentManager.ReadModelCards(Doc);
      if (serializedModelCards == null)
      {
        Models = new System.Collections.Generic.List<ModelCard>();
        return;
      }
      Models = Deserialize(serializedModelCards);
    }

    public override void WriteToFile()
    {
      string modelCardsString = Serialize();
      AutocadDocumentManager.WriteModelCards(Doc, modelCardsString);
    }
  }
}
