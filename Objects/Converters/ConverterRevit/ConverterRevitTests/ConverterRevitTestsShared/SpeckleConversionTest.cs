using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using ConnectorRevit.Storage;
using DesktopUI2.Models;
using Objects.Converter.Revit;
using RevitSharedResources.Models;
using Speckle.ConnectorRevit.UI;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Xunit;
using xUnitRevitUtils;
using DB = Autodesk.Revit.DB;

namespace ConverterRevitTests;

public class SpeckleConversionTest
{
  internal SpeckleConversionFixture fixture;

  public SpeckleConversionTest(SpeckleConversionFixture fixture)
  {
    this.fixture = fixture;
    this.fixture.TestClassName = GetType().Name;
  }

  internal async Task NativeToSpeckle()
  {
    ConverterRevit converter = new();
    converter.SetContextDocument(fixture.SourceDoc);
    converter.SetContextDocument(new RevitDocumentAggregateCache(new UIDocumentProvider(xru.Uiapp)));

    foreach (var elem in fixture.RevitElements)
    {
      await APIContext.Run(() =>
      {
        var spkElem = converter.ConvertToSpeckle(elem);

        if (spkElem is Base re)
        {
          AssertUtils.ValidSpeckleElement(elem, re);
        }
      });
    }
    Assert.Equal(0, converter.Report.ConversionErrorsCount);
  }

  /// <summary>
  /// Gets elements from the fixture SourceDoc
  /// Converts them to Speckle
  /// Creates a new Doc (or uses the open one if open!)
  /// Converts the speckle objects to Native
  /// </summary>
  /// <typeparam name="T"></typeparam>
  /// <param name="assert"></param>
  internal async Task<List<ApplicationObject>> SpeckleToNative<T>(
    Action<T, T> assert,
    Func<T, T, Task> assertAsync = null,
    UpdateData ud = null
  )
  {
    Document doc = null;
    IList<Element> elements = null;
    List<ApplicationObject> appPlaceholders = null;

    if (ud == null)
    {
      doc = fixture.SourceDoc;
      elements = fixture.RevitElements;
    }
    else
    {
      doc = ud.Doc;
      elements = ud.Elements;
      appPlaceholders = ud.AppPlaceholders;

      var updateElementTestNumberMap = new Dictionary<int, string>();
      foreach (var element in elements)
      {
        var testNumber = SpeckleUtils.GetSpeckleObjectTestNumber(element);
        if (testNumber > 0)
        {
          try
          {
            updateElementTestNumberMap.Add(testNumber, element.UniqueId);
          }
          catch (ArgumentException e)
          {
            // there are duplicate SpeckleObjectTestNumber values in the update document
            throw;
          }
        }
      }

      foreach (var appObj in appPlaceholders)
      {
        if (!(appObj.Converted.FirstOrDefault() is DB.Element element))
        {
          continue;
        }

        var testNumber = SpeckleUtils.GetSpeckleObjectTestNumber(element);
        if (testNumber == 0)
        {
          continue;
        }

        if (updateElementTestNumberMap.TryGetValue(testNumber, out var toNativeElementId))
        {
          appObj.applicationId = toNativeElementId;
        }
      }
    }

    ConverterRevit converter = new();
    converter.SetContextDocument(doc);
    converter.SetContextDocument(new RevitDocumentAggregateCache(new UIDocumentProvider(xru.Uiapp)));
    //setting context objects for nested routine
    var contextObjects = elements
      .Select(obj => new ApplicationObject(obj.UniqueId, obj.GetType().ToString()) { applicationId = obj.UniqueId })
      .ToList();
    converter.SetContextObjects(contextObjects);
    converter.SetContextDocument(new StreamStateCache(new StreamState()));

    var spkElems = new List<Base>();
    await APIContext
      .Run(() =>
      {
        foreach (var elem in elements)
        {
          bool isAlreadyConverted = ConnectorBindingsRevit.GetOrCreateApplicationObject(
            elem,
            converter.Report,
            out ApplicationObject reportObj
          );
          if (isAlreadyConverted)
          {
            continue;
          }

          var conversionResult = converter.ConvertToSpeckle(elem);
          if (conversionResult != null)
          {
            spkElems.Add(conversionResult);
          }
        }
      })
      .ConfigureAwait(false);

    converter = new ConverterRevit();
    converter.ReceiveMode = Speckle.Core.Kits.ReceiveMode.Update;

    converter.SetContextDocument(fixture.NewDoc);
    converter.SetContextDocument(new RevitDocumentAggregateCache(new UIDocumentProvider(xru.Uiapp)));
    //setting context objects for update routine
    var state = new StreamState() { ReceivedObjects = appPlaceholders ?? new List<ApplicationObject>() };
    converter.SetContextDocument(new StreamStateCache(state));
    converter.SetContextDocument(new ConvertedObjectsCache());

    var contextObjs = spkElems
      .Select(x => new ApplicationObject(x.id, x.speckle_type) { applicationId = x.applicationId })
      .ToList();
    var appObjs = new List<ApplicationObject>();
    foreach (var contextObj in contextObjs)
    {
      if (string.IsNullOrEmpty(contextObj.applicationId) && string.IsNullOrEmpty(contextObj.OriginalId))
      {
        continue;
      }

      appObjs.Add(contextObj);
    }

    converter.SetContextObjects(appObjs);

    var resEls = new List<ApplicationObject>();
    //used to associate th nested Base objects with eh flat revit ones
    var flatSpkElems = new List<Base>();

    await SpeckleUtils.RunInTransaction(
      () =>
      {
        //xru.RunInTransaction(() =>
        //{
        foreach (var el in spkElems)
        {
          object res = null;
          try
          {
            res = converter.ConvertToNative(el);
          }
          catch (Exception ex) when (!ex.IsFatal())
          {
            converter.Report.LogConversionError(ex);
          }

          if (res is List<ApplicationObject> apls)
          {
            resEls.AddRange(apls);
            flatSpkElems.Add(el);
            if (el["elements"] == null)
            {
              continue;
            }

            flatSpkElems.AddRange((el["elements"] as List<Base>).Where(b => converter.CanConvertToNative(b)));
          }
          else if (res is ApplicationObject appObj)
          {
            resEls.Add(appObj);
            flatSpkElems.Add(el);
          }
          else if (res == null)
          {
            throw new Exception("Conversion returned null");
          }
          else
          {
            throw new Exception(
              $"Conversion of Speckle object, of type {el.speckle_type}, to Revit returned unexpected type, {res.GetType().FullName}"
            );
          }
        }
        //}, fixture.NewDoc).Wait();
      },
      fixture.NewDoc,
      converter
    );

    Assert.Equal(0, converter.Report.ConversionErrorsCount);

    for (var i = 0; i < spkElems.Count; i++)
    {
      var sourceElem = (T)(object)elements.FirstOrDefault(x => x.UniqueId == flatSpkElems[i].applicationId);
      var destElement = (T)((ApplicationObject)resEls[i]).Converted.FirstOrDefault();

      assert?.Invoke(sourceElem, destElement);
      if (assertAsync != null)
      {
        await assertAsync.Invoke(sourceElem, destElement);
      }
    }

    if (!fixture.UpdateTestRunning)
    {
      SpeckleUtils.DeleteElement(resEls);
    }

    return resEls;
  }

  /// <summary>
  /// Runs SpeckleToNative with SourceDoc and UpdatedDoc
  /// </summary>
  /// <typeparam name="T"></typeparam>
  /// <param name="assert"></param>
  internal async Task SpeckleToNativeUpdates<T>(Action<T, T> assert, Func<T, T, Task> assertAsync = null)
  {
    fixture.UpdateTestRunning = true;
    var initialObjs = await SpeckleToNative(assert, assertAsync);
    var updatedObjs = await SpeckleToNative(
      assert,
      assertAsync,
      new UpdateData
      {
        AppPlaceholders = initialObjs.Cast<ApplicationObject>().ToList(),
        Doc = fixture.UpdatedDoc,
        Elements = fixture.UpdatedRevitElements
      }
    );
    fixture.UpdateTestRunning = false;

    // delete the elements that were not being deleted during the update test
    SpeckleUtils.DeleteElement(initialObjs);
    //DeleteElement(updatedObjs);
  }

  internal async Task SelectionToNative<T>(Action<T, T> assert, Func<T, T, Task> assertAsync = null)
  {
    ConverterRevit converter = new();
    converter.SetContextDocument(fixture.SourceDoc);
    converter.SetContextDocument(new RevitDocumentAggregateCache(new UIDocumentProvider(xru.Uiapp)));
    var spkElems = fixture.Selection.Select(x => converter.ConvertToSpeckle(x) as Base).ToList();

    converter = new ConverterRevit();
    converter.SetContextDocument(fixture.NewDoc);
    converter.SetContextDocument(new StreamStateCache(new StreamState()));
    converter.SetContextDocument(new RevitDocumentAggregateCache(new UIDocumentProvider(xru.Uiapp)));
    converter.SetContextDocument(new ConvertedObjectsCache());
    var revitEls = new List<object>();

    await SpeckleUtils.RunInTransaction(
      () =>
      {
        //xru.RunInTransaction(() =>
        //{
        foreach (var el in spkElems)
        {
          var res = converter.ConvertToNative(el);
          if (res is List<ApplicationObject> apls)
          {
            revitEls.AddRange(apls);
          }
          else
          {
            revitEls.Add(res);
          }
        }
        //}, fixture.NewDoc).Wait();
      },
      fixture.NewDoc,
      converter
    );

    Assert.Equal(0, converter.Report.ConversionErrorsCount);

    for (var i = 0; i < revitEls.Count; i++)
    {
      var sourceElem = (T)(object)fixture.Selection[i];
      var destElement = (T)((ApplicationObject)revitEls[i]).Converted.FirstOrDefault();
      assert?.Invoke(sourceElem, destElement);
      if (assertAsync != null)
      {
        await assertAsync.Invoke(sourceElem, destElement);
      }
    }
    SpeckleUtils.DeleteElement(revitEls);
  }
}

public class UpdateData
{
  public Document Doc { get; set; }
  public IList<Element> Elements { get; set; }
  public List<ApplicationObject> AppPlaceholders { get; set; }
}
