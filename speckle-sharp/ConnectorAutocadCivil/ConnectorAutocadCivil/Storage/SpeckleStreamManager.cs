using System;
using System.Collections.Generic;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;

using DesktopUI2.Models;
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

  /// <summary>
  /// Manages the serialisation of speckle stream state
  /// (stream info, account info, and filter type) in an autocad document.
  /// </summary>
  /// <remarks>
  /// Uses a child dictionary for custom data in the Named Object Dictionary (NOD) which is the root level dictionary.
  /// This is because NOD persists after a document is closed (unlike file User Data).
  /// Custom data is stored as XRecord key value entries of type (string, ResultBuffer).
  /// ResultBuffers are TypedValue arrays, with the DxfCode of the input type as an integer.
  /// Used for DesktopUI2
  /// </remarks>
  public static class SpeckleStreamManager2
  {
    readonly static string SpeckleExtensionDictionary = "Speckle";
    readonly static string SpeckleStreamStates = "StreamStates";

    private static Xrecord GetSpeckleStreamRecord(Document doc)
    {
      Xrecord record = null;
      using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
      {
        var NOD = (DBDictionary)tr.GetObject(doc.Database.NamedObjectsDictionaryId, OpenMode.ForRead);
        if (NOD.Contains(SpeckleExtensionDictionary))
        {
          var speckleDict = tr.GetObject(NOD.GetAt(SpeckleExtensionDictionary), OpenMode.ForRead) as DBDictionary;
          if (speckleDict != null && speckleDict.Count > 0)
          {
            foreach (DBDictionaryEntry entry in speckleDict)
            {
              if (entry.Key == SpeckleStreamStates)
              {
                record = tr.GetObject(entry.Value, OpenMode.ForRead) as Xrecord;
              }
            }
          }
        }
        tr.Commit();
      }
      return record;
    }

    /// <summary>
    /// Returns all the speckle stream states present in the current document.
    /// </summary>
    /// <param name="doc"></param>
    /// <returns></returns>
    public static List<StreamState> ReadState(Document doc)
    {
      try
      {
        var streamStatesRecord = GetSpeckleStreamRecord(doc);
        if (streamStatesRecord == null)
          return new List<StreamState>();

        var str = streamStatesRecord.Data.AsArray()[0].Value as string;
        var states = JsonConvert.DeserializeObject<List<StreamState>>(str);

        return states;
      }
      catch (Exception e)
      {
        return new List<StreamState>();
      }
    }

    /// <summary>
    /// Writes the stream states to the current document.
    /// </summary>
    /// <param name="doc"></param>
    /// <param name="wrap"></param>
    public static void WriteStreamStateList(Document doc, List<StreamState> streamStates)
    {
      if (doc == null)
        return;

      using (DocumentLock l = doc.LockDocument())
      {
        using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
        {
          var NOD = (DBDictionary)tr.GetObject(doc.Database.NamedObjectsDictionaryId, OpenMode.ForRead);
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
          var value = JsonConvert.SerializeObject(streamStates) as string;
          xRec.Data = new ResultBuffer(new TypedValue(Convert.ToInt32(DxfCode.Text), value));
          speckleDict.SetAt(SpeckleStreamStates, xRec);
          tr.AddNewlyCreatedDBObject(xRec, true);
          tr.Commit();
        }
      }
    }
  }
}
