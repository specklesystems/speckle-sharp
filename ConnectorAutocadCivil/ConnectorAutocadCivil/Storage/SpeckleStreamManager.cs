using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;

using DesktopUI2.Models;
using Speckle.Newtonsoft.Json;

namespace Speckle.ConnectorAutocadCivil.Storage
{
  /// <summary>
  /// Manages the serialisation of speckle stream state
  /// </summary>
  /// <remarks>
  /// Uses a child dictionary for custom data in the Named Object Dictionary (NOD) which is the root level dictionary.
  /// This is because NOD persists after a document is closed (unlike file User Data).
  /// Custom data is stored as XRecord key value entries of type (string, ResultBuffer).
  /// ResultBuffers are TypedValue arrays, with the DxfCode of the input type as an integer.
  /// Used for DesktopUI2
  /// </remarks>
  public static class SpeckleStreamManager
  {
    readonly static string SpeckleExtensionDictionary = "Speckle";
    readonly static string SpeckleStreamStates = "StreamStates";

    /// <summary>
    /// Returns all the speckle stream states present in the current document.
    /// </summary>
    /// <param name="doc"></param>
    /// <returns></returns>
    public static List<StreamState> ReadState(Document doc)
    {
      var streams = new List<StreamState>();

      if (doc == null)
        return streams;

      using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
      {
        var NOD = (DBDictionary)tr.GetObject(doc.Database.NamedObjectsDictionaryId, OpenMode.ForRead);
        if (NOD.Contains(SpeckleExtensionDictionary))
        {
          var speckleDict = tr.GetObject(NOD.GetAt(SpeckleExtensionDictionary), OpenMode.ForRead) as DBDictionary;
          if (speckleDict != null && speckleDict.Count > 0)
          {
            var id = speckleDict.GetAt(SpeckleStreamStates);
            if (id != ObjectId.Null)
            {
              try // careful here: entries are length-capped and a serialized streamstate string could've been cut off, resulting in crash on deserialize
              {
                var record = tr.GetObject(id, OpenMode.ForRead) as Xrecord;
                streams = JsonConvert.DeserializeObject<List<StreamState>>(record.Data.AsArray()[0].Value as string);
              }
              catch (Exception e)
              { }
            }
          }
        }
        tr.Commit();
      }

      return streams;
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

      //fix for metadata length limitation issues https://github.com/specklesystems/speckle-sharp/issues/2030
      foreach (var streamState in streamStates)
      {
        streamState.CachedStream.collaborators = new List<Core.Api.Collaborator>();
      }

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
