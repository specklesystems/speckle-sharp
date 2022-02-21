using System;
using System.Collections.Generic;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;

using Speckle.Newtonsoft.Json;

namespace Speckle.ConnectorAutocadCivil.Storage
{
  /// <summary>
  /// Manages streams stored in an autocad document
  /// </summary>
  /// <remarks>
  /// Uses a child dictionary for custom data in the Named Object Dictionary (NOD) which is the root level dictionary.
  /// This is because NOD persists after a document is closed (unlike file User Data).
  /// Custom data is stored as XRecord key value entries of type (string, ResultBuffer).
  /// ResultBuffers are TypedValue arrays, with the DxfCode of the input type as an integer.
  /// </remarks>
  public static class SpeckleStreamManager
  {
    readonly static string SpeckleExtensionDictionary = "SpeckleStreams";

    public static List<string> GetSpeckleStreams()
    {
      Document Doc = Application.DocumentManager.MdiActiveDocument;
      var streams = new List<string>();

      if (Doc == null)
        return streams;

      using (Transaction tr = Doc.Database.TransactionManager.StartTransaction())
      {
        var NOD = (DBDictionary)tr.GetObject(Doc.Database.NamedObjectsDictionaryId, OpenMode.ForRead);
        if (NOD.Contains(SpeckleExtensionDictionary))
        {
          var speckleDict = tr.GetObject(NOD.GetAt(SpeckleExtensionDictionary), OpenMode.ForRead) as DBDictionary;
          if (speckleDict != null && speckleDict.Count > 0)
          {
            foreach (DBDictionaryEntry entry in speckleDict)
            {
              var value = tr.GetObject(entry.Value, OpenMode.ForRead) as Xrecord;
              streams.Add(value.Data.AsArray()[0].Value as string);
            }
          }
        }
        tr.Commit();
      }
      return streams;
    }

    public static void AddSpeckleStream(string id, string stream)
    {
      Document Doc = Application.DocumentManager.MdiActiveDocument;
      if (Doc == null)
        return;
      using (DocumentLock l = Doc.LockDocument())
      {
        using (Transaction tr = Doc.Database.TransactionManager.StartTransaction())
        {
          var NOD = (DBDictionary)tr.GetObject(Doc.Database.NamedObjectsDictionaryId, OpenMode.ForRead);
          DBDictionary speckleDict;
          if (NOD.Contains(SpeckleExtensionDictionary))
          {
            speckleDict = (DBDictionary)tr.GetObject(NOD.GetAt(SpeckleExtensionDictionary), OpenMode.ForWrite);
          }
          else
          {
            speckleDict = new DBDictionary();
            NOD.UpgradeOpen();
            NOD.SetAt(SpeckleExtensionDictionary, speckleDict);
            tr.AddNewlyCreatedDBObject(speckleDict, true);
          }
          var xRec = new Xrecord();
          xRec.Data = new ResultBuffer(new TypedValue(Convert.ToInt32(DxfCode.Text), stream));
          speckleDict.SetAt(id, xRec);
          tr.AddNewlyCreatedDBObject(xRec, true);
          tr.Commit();
        }
      }
    }

    public static void RemoveSpeckleStream(string id)
    {
      Document Doc = Application.DocumentManager.MdiActiveDocument;
      if (Doc == null)
        return;
      using (DocumentLock l = Doc.LockDocument())
      {
        using (Transaction tr = Doc.Database.TransactionManager.StartTransaction())
        {
          var NOD = (DBDictionary)tr.GetObject(Doc.Database.NamedObjectsDictionaryId, OpenMode.ForRead);
          if (NOD.Contains(SpeckleExtensionDictionary))
          {
            var speckleDict = (DBDictionary)tr.GetObject(NOD.GetAt(SpeckleExtensionDictionary), OpenMode.ForWrite);
            speckleDict.Remove(id);
          }
          tr.Commit();
        }
      }
    }

    public static void UpdateSpeckleStream(string id, string stream)
    {
      Document Doc = Application.DocumentManager.MdiActiveDocument;
      if (Doc == null)
        return;
      using (DocumentLock l = Doc.LockDocument())
      {
        using (Transaction tr = Doc.Database.TransactionManager.StartTransaction())
        {
          var NOD = (DBDictionary)tr.GetObject(Doc.Database.NamedObjectsDictionaryId, OpenMode.ForRead);
          if (NOD.Contains(SpeckleExtensionDictionary))
          {
            var speckleDict = (DBDictionary)tr.GetObject(NOD.GetAt(SpeckleExtensionDictionary), OpenMode.ForWrite);
            var xRec = new Xrecord();
            xRec.Data = new ResultBuffer(new TypedValue(Convert.ToInt32(DxfCode.Text), stream));
            speckleDict.SetAt(id, xRec);
            tr.AddNewlyCreatedDBObject(xRec, true);
          }
          tr.Commit();
        }
      }
    }
  }
}
