using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;

using DesktopUI2.Models;
using Speckle.ConnectorAutocadCivil.DocumentUtils;
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

      using (TransactionContext.StartTransaction(doc))
      {
        Transaction tr = doc.Database.TransactionManager.TopTransaction;
        var NOD = (DBDictionary)tr.GetObject(doc.Database.NamedObjectsDictionaryId, OpenMode.ForRead);
        if (!NOD.Contains(SpeckleExtensionDictionary))
          return streams;

        var speckleDict = tr.GetObject(NOD.GetAt(SpeckleExtensionDictionary), OpenMode.ForRead) as DBDictionary;
        if (speckleDict == null || speckleDict.Count == 0)
          return streams;

        var id = speckleDict.GetAt(SpeckleStreamStates);
        if (id == ObjectId.Null)
          return streams;

        var record = tr.GetObject(id, OpenMode.ForRead) as Xrecord;
        var value = GetXrecordData(record);

        try
        {
          //Try to decode here because there is old data
          value = Base64Decode(value);
        }
        catch (Exception e)
        { }

        streams = JsonConvert.DeserializeObject<List<StreamState>>(value);

        return streams;
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

      using (TransactionContext.StartTransaction(doc))
      {
        Transaction tr = doc.Database.TransactionManager.TopTransaction;
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
        xRec.Data = CreateResultBuffer(value);
        speckleDict.SetAt(SpeckleStreamStates, xRec);
        tr.AddNewlyCreatedDBObject(xRec, true);
      }
    }

    private static ResultBuffer CreateResultBuffer(string value)
    {
      int size = 1024;
      var valueEncoded = Base64Encode(value);
      var valueEncodedList = SplitString(valueEncoded, size);

      ResultBuffer rb = new ResultBuffer();

      foreach (var valueEncodedSplited in valueEncodedList)
      {
        rb.Add(new TypedValue((int)DxfCode.Text, valueEncodedSplited));
      }

      return rb;
    }

    private static string GetXrecordData(Xrecord pXrecord)
    {
      StringBuilder valueEncoded = new StringBuilder();
      foreach (TypedValue typedValue in pXrecord.Data)
      {
        if (typedValue.TypeCode == (int)DxfCode.Text)
        {
          valueEncoded.Append(typedValue.Value.ToString());
        }
      }

      return valueEncoded.ToString();
    }

    private static string Base64Encode(string plainText)
    {
      var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
      return System.Convert.ToBase64String(plainTextBytes);
    }

    private static string Base64Decode(string base64EncodedData)
    {
      var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
      return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
    }

    private static IEnumerable<string> SplitString(string text, int chunkSize)
    {
      for (int offset = 0; offset < text.Length; offset += chunkSize)
      {
        int size = Math.Min(chunkSize, text.Length - offset);
        yield return text.Substring(offset, size);
      }
    }

  }
}
