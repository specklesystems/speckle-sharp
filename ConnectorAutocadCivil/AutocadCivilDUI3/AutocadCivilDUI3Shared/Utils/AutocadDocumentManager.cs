using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;

namespace AutocadCivilDUI3Shared.Utils;

public static class AutocadDocumentManager
{
  private const string SPECKLE_KEY = "Speckle_DUI3";
  private const string SPECKLE_MODEL_CARDS_KEY = "Speckle_DUI3_Model_Cards";

  /// <summary>
  /// Returns all the speckle model cards present in the current document.
  /// </summary>
  /// <param name="doc"></param>
  /// <returns></returns>
  public static string ReadModelCards(Document doc)
  {
    if (doc == null)
    {
      return null;
    }

    using (TransactionContext.StartTransaction(doc))
    {
      Transaction tr = doc.Database.TransactionManager.TopTransaction;
      DBDictionary nod = (DBDictionary)tr.GetObject(doc.Database.NamedObjectsDictionaryId, OpenMode.ForRead);
      if (!nod.Contains(SPECKLE_KEY))
      {
        return null;
      }

      DBDictionary speckleDict = tr.GetObject(nod.GetAt(SPECKLE_KEY), OpenMode.ForRead) as DBDictionary;
      if (speckleDict == null || speckleDict.Count == 0)
      {
        return null;
      }

      ObjectId id = speckleDict.GetAt(SPECKLE_MODEL_CARDS_KEY);
      if (id == ObjectId.Null)
      {
        return null;
      }

      Xrecord record = tr.GetObject(id, OpenMode.ForRead) as Xrecord;
      string value = GetXrecordData(record);

      try
      {
        //Try to decode here because there is old data
        return Base64Decode(value);
      }
      catch (ApplicationException e)
      {
        Debug.WriteLine(e);
        return null;
      }
    }
  }

  /// <summary>
  /// Writes the model cards to the current document.
  /// </summary>
  /// <param name="doc"></param>
  /// <param name="wrap"></param>
  public static void WriteModelCards(Document doc, string modelCardsString)
  {
    if (doc == null)
    {
      return;
    }

    using (TransactionContext.StartTransaction(doc))
    {
      Transaction tr = doc.Database.TransactionManager.TopTransaction;
      var nod = (DBDictionary)tr.GetObject(doc.Database.NamedObjectsDictionaryId, OpenMode.ForRead);
      DBDictionary speckleDict;
      if (nod.Contains(SPECKLE_KEY))
      {
        speckleDict = (DBDictionary)tr.GetObject(nod.GetAt(SPECKLE_KEY), OpenMode.ForWrite);
      }
      else
      {
        speckleDict = new DBDictionary();
        nod.UpgradeOpen();
        nod.SetAt(SPECKLE_KEY, speckleDict);
        tr.AddNewlyCreatedDBObject(speckleDict, true);
      }
      Xrecord xRec = new();
      xRec.Data = CreateResultBuffer(modelCardsString);
      speckleDict.SetAt(SPECKLE_MODEL_CARDS_KEY, xRec);
      tr.AddNewlyCreatedDBObject(xRec, true);
    }
  }

  private static ResultBuffer CreateResultBuffer(string value)
  {
    int size = 1024;
    var valueEncoded = Base64Encode(value);
    var valueEncodedList = SplitString(valueEncoded, size);

    ResultBuffer rb = new();

    foreach (string valueEncodedSplit in valueEncodedList)
    {
      rb.Add(new TypedValue((int)DxfCode.Text, valueEncodedSplit));
    }

    return rb;
  }

  private static string GetXrecordData(Xrecord pXrecord)
  {
    StringBuilder valueEncoded = new();
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
