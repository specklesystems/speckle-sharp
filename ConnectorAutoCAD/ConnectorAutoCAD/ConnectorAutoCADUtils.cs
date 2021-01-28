using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Linq;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;

namespace Speckle.ConnectorAutoCAD
{
  /* DEPRECATED - using user data instead of named object dictionary for storing streams ... keep code just in case we need this infor later (eg object xdata for revit direct conversions)
  public static class ConnectorAutoCADUtils
  {
    public static Document Doc => Application.DocumentManager.MdiActiveDocument;
    private static string SpeckleExtensionDictionary = "Speckle";


    // AutoCAD ogranizes information in the Named Object Dictionary (NOD) which is the root level dictionary
    // Users can create child dictionaries in the Named Object Dictionary for custom data.
    // Custom data is stored as XRecord key value entries of type (string, ResultBuffer).
    // ResultBuffers are TypedValue arrays, with the DxfCode of the input type as an integer.

    // Notes on disposing in AutoCAD: https://www.keanw.com/2008/06/cleaning-up-aft.html
    public static List<string> GetSpeckleDictStreams()
    {
      Database db = Doc.Database;
      List<string> streams = new List<string>();
      using (Transaction tr = db.TransactionManager.StartTransaction())
      {
        DBDictionary NOD = (DBDictionary)tr.GetObject(db.NamedObjectsDictionaryId, OpenMode.ForRead);
        if (!NOD.Contains(SpeckleExtensionDictionary))
          return null;
        DBDictionary speckleDict = tr.GetObject(NOD.GetAt(SpeckleExtensionDictionary), OpenMode.ForRead) as DBDictionary;
        if (speckleDict == null || speckleDict.Count == 0)
          return null;
        foreach (DBDictionaryEntry entry in speckleDict)
        {
          Xrecord value = tr.GetObject(entry.Value, OpenMode.ForRead) as Xrecord;
          streams.Add(value.Data.AsArray()[0].Value as string);
        }
      }
      return streams;
    }

    public static void AddStreamToSpeckleDict(string id, string stream)
    {
      Database db = Doc.Database;

      using (Transaction tr = db.TransactionManager.StartTransaction())
      {
        DBDictionary NOD = (DBDictionary)tr.GetObject(db.NamedObjectsDictionaryId, OpenMode.ForRead);
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
        Xrecord xRec = new Xrecord();
        xRec.Data = new ResultBuffer(new TypedValue(Convert.ToInt32(DxfCode.Text),stream));
        speckleDict.SetAt(id, null);
        tr.AddNewlyCreatedDBObject(xRec, true);
        tr.Commit();
      }
    }

    public static void RemoveStreamFromSpeckleDict(string id)
    {
      Database db = Doc.Database;

      using (Transaction tr = db.TransactionManager.StartTransaction())
      {
        DBDictionary NOD = (DBDictionary)tr.GetObject(db.NamedObjectsDictionaryId, OpenMode.ForRead);
        if (NOD.Contains(SpeckleExtensionDictionary))
        {
          DBDictionary speckleDict = (DBDictionary)tr.GetObject(NOD.GetAt(SpeckleExtensionDictionary), OpenMode.ForWrite);
          speckleDict.Remove(id);
        }
        tr.Commit();
      }
    }

    public static void UpdateStreamInSpeckleDict(string id, string stream)
    {
      Database db = Doc.Database;

      using (Transaction tr = db.TransactionManager.StartTransaction())
      {
        DBDictionary NOD = (DBDictionary)tr.GetObject(db.NamedObjectsDictionaryId, OpenMode.ForRead);
        if (NOD.Contains(SpeckleExtensionDictionary))
        {
          DBDictionary speckleDict = (DBDictionary)tr.GetObject(NOD.GetAt(SpeckleExtensionDictionary), OpenMode.ForWrite);
          speckleDict[id] = new ResultBuffer(new TypedValue(Convert.ToInt32(DxfCode.Text), stream));
        }
        tr.Commit();
      }
    }
  }
  */

  public class UserDataClass
  {
    public static Document Doc => Application.DocumentManager.MdiActiveDocument;

    // Specify a key under which we want to store our custom data
    const string SpeckleKey = "Speckle";

    // Define a class for our custom data
    public class SpeckleStreams
    {
      public Dictionary<string, string> Streams;
      public SpeckleStreams(Dictionary<string, string> inputDict = null)
      {
        if (inputDict != null)
          Streams = inputDict;
        else
          Streams = new Dictionary<string, string>();
      }

      public void AddOrUpdateEntry(string id, string stream)
      {
        if (Streams.ContainsKey(id))
          Streams[id] = stream;
        else
          Streams.Add(id, stream);
      }

      public void RemoveEntry(string id)
      {
        if (Streams.ContainsKey(id))
          Streams.Remove(id);
      }
    }

    public static List<string> GetSpeckleDictStreams()
    {
      SpeckleStreams streams = Doc.UserData[SpeckleKey] as SpeckleStreams;
      if (streams == null)
        return new List<string>();
      else
        return streams.Streams.Values.ToList();
    }

    public static void AddStreamToSpeckleDict(string id, string stream)
    {
      SpeckleStreams streams = Doc.UserData[SpeckleKey] as SpeckleStreams;
      if (streams == null)
      {
        streams = new SpeckleStreams(new Dictionary<string, string>() { { id, stream }, });
        Doc.UserData.Add(SpeckleKey, streams);
      }
      else
      {
        streams.AddOrUpdateEntry(id, stream);
        Doc.UserData[SpeckleKey] = streams;
      }
    }

    public static void RemoveStreamFromSpeckleDict(string id)
    {
      SpeckleStreams streams = Doc.UserData[SpeckleKey] as SpeckleStreams;
      streams.RemoveEntry(id);
      Doc.UserData[SpeckleKey] = streams;
    }

    public static void UpdateStreamInSpeckleDict(string id, string stream)
    {
      SpeckleStreams streams = Doc.UserData[SpeckleKey] as SpeckleStreams;
      streams.AddOrUpdateEntry(id, stream);
      Doc.UserData[SpeckleKey] = streams;
    }
  }

  // layer and block transaction wrappers
}
