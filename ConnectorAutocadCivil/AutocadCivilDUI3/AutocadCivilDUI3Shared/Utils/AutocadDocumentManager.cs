using System;
using System.Collections.Generic;
using System.Text;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using DUI3.Models;
using Speckle.Newtonsoft.Json;

namespace AutocadCivilDUI3Shared.Utils
{
  public static class AutocadDocumentManager
  {
    private static string SpeckleKey = "Speckle_DUI3";
    private static string SpeckleModelCardsKey = "Speckle_DUI3_Model_Cards";

    /// <summary>
    /// Returns all the speckle stream states present in the current document.
    /// </summary>
    /// <param name="doc"></param>
    /// <returns></returns>
    public static string ReadModelCards(Document doc)
    {
      if (doc == null)
        return null;

      using (TransactionContext.StartTransaction(doc))
      {
        Transaction tr = doc.Database.TransactionManager.TopTransaction;
        var NOD = (DBDictionary)tr.GetObject(doc.Database.NamedObjectsDictionaryId, OpenMode.ForRead);
        if (!NOD.Contains(SpeckleKey))
          return null;

        var speckleDict = tr.GetObject(NOD.GetAt(SpeckleKey), OpenMode.ForRead) as DBDictionary;
        if (speckleDict == null || speckleDict.Count == 0)
          return null;

        var id = speckleDict.GetAt(SpeckleModelCardsKey);
        if (id == ObjectId.Null)
          return null;

        var record = tr.GetObject(id, OpenMode.ForRead) as Xrecord;
        var value = GetXrecordData(record);

        try
        {
          //Try to decode here because there is old data
          return Base64Decode(value);
        }
        catch (Exception e)
        { 
          return null;
        }
      }
    }

    /// <summary>
    /// Writes the stream states to the current document.
    /// </summary>
    /// <param name="doc"></param>
    /// <param name="wrap"></param>
    public static void WriteModelCards(Document doc, string modelCardsString)
    {
      if (doc == null)
        return;

      using (TransactionContext.StartTransaction(doc))
      {
        Transaction tr = doc.Database.TransactionManager.TopTransaction;
        var NOD = (DBDictionary)tr.GetObject(doc.Database.NamedObjectsDictionaryId, OpenMode.ForRead);
        DBDictionary speckleDict;
        if (NOD.Contains(SpeckleKey))
        {
          speckleDict = (DBDictionary)tr.GetObject(NOD.GetAt(SpeckleKey), OpenMode.ForWrite);
        }
        else
        {
          speckleDict = new DBDictionary();
          NOD.UpgradeOpen();
          NOD.SetAt(SpeckleKey, speckleDict);
          tr.AddNewlyCreatedDBObject(speckleDict, true);
        }
        var xRec = new Xrecord();
        xRec.Data = CreateResultBuffer(modelCardsString);
        speckleDict.SetAt(SpeckleModelCardsKey, xRec);
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
