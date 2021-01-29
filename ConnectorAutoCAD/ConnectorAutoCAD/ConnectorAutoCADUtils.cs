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
    const string StreamKey = "SpeckleStreams";
    const string SelectionKey = "SpeckleSelection";
    public enum SelectionState { Current, Previous, None};

    #region streams
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
      SpeckleStreams streams = Doc.UserData[StreamKey] as SpeckleStreams;
      if (streams == null)
        return new List<string>();
      else
        return streams.Streams.Values.ToList();
    }

    public static void AddStreamToSpeckleDict(string id, string stream)
    {
      SpeckleStreams streams = Doc.UserData[StreamKey] as SpeckleStreams;
      if (streams == null)
      {
        streams = new SpeckleStreams(new Dictionary<string, string>() { { id, stream }, });
        Doc.UserData.Add(StreamKey, streams);
      }
      else
      {
        streams.AddOrUpdateEntry(id, stream);
        Doc.UserData[StreamKey] = streams;
      }
    }

    public static void RemoveStreamFromSpeckleDict(string id)
    {
      SpeckleStreams streams = Doc.UserData[StreamKey] as SpeckleStreams;
      if (streams != null)
      {
        streams.RemoveEntry(id);
        Doc.UserData[StreamKey] = streams;
      }
    }

    public static void UpdateStreamInSpeckleDict(string id, string stream)
    {
      SpeckleStreams streams = Doc.UserData[StreamKey] as SpeckleStreams;
      if (streams != null)
      {
        streams.AddOrUpdateEntry(id, stream);
        Doc.UserData[StreamKey] = streams;
      }
    }
    #endregion

    #region selection

    // currently the replace existing bool isn't used - placeholder functionality in case this is needed later, remove during refactoring if not
    // right now, the only "state" in use is "current", and remove method isn't implemented either. just keeping it in for CRUD consistency with stream class, cleanup later if not used.
    public class SpeckleSelections
    {
      public Dictionary<string, List<string>> Selections;
      public SpeckleSelections(Dictionary<string, List<string>> inputDict = null)
      {
        if (inputDict != null)
          Selections = inputDict;
        else
          Selections = new Dictionary<string, List<string>>();
      }

      public void AddOrUpdateEntry(string state, List<string> objectIds, bool replaceExisting)
      {
        if (Selections.ContainsKey(state))
          if (replaceExisting)
            Selections[state] = objectIds;
          else
          {
            List<string> stateIds = Selections[state];
            List<string> idsToAdd = objectIds.Where(o => !Selections[state].Contains(o)).ToList();
            stateIds.AddRange(idsToAdd);
            Selections[state] = stateIds;
          }
        else
          Selections.Add(state, objectIds);
      }

      public void RemoveEntry(string state)
      {
        if (Selections.ContainsKey(state))
          Selections.Remove(state);
      }
    }

    public static List<string> GetSpeckleDictSelection(SelectionState state)
    {
      SpeckleSelections selections = Doc.UserData[SelectionKey] as SpeckleSelections;
      if (selections == null)
        return new List<string>();
      else
        return selections.Selections[state.ToString()].ToList();
    }

    public static void AddSelectionToSpeckleDict(SelectionState state, List<string> objectIds, bool replaceExisting = true)
    {
      SpeckleSelections selections = Doc.UserData[SelectionKey] as SpeckleSelections;
      if (selections == null)
      {
        selections = new SpeckleSelections(new Dictionary<string, List<string>>() { { state.ToString(), objectIds }, });
        Doc.UserData.Add(SelectionKey, selections);
      }
      else
      {
        selections.AddOrUpdateEntry(state.ToString(), objectIds, replaceExisting);
        Doc.UserData[SelectionKey] = selections;
      }
    }

    public static void RemoveSelectionFromSpeckleDict(SelectionState state)
    {
      SpeckleSelections selections = Doc.UserData[SelectionKey] as SpeckleSelections;
      if (selections != null)
      {
        selections.RemoveEntry(state.ToString());
        Doc.UserData[SelectionKey] = selections;
      }
    }

    public static void UpdateSelectionInSpeckleDict(SelectionState state, List<string> objectIds, bool replaceExisting = true)
    {
      SpeckleSelections selections = Doc.UserData[SelectionKey] as SpeckleSelections;
      if (selections != null)
      {
        selections.AddOrUpdateEntry(state.ToString(), objectIds, replaceExisting);
        Doc.UserData[SelectionKey] = selections;
      }
    }
    #endregion
  }

}
