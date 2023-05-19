using Autodesk.Revit.DB;
using Objects.BuiltElements.Revit;
using Objects.Converter.Revit;
using Revit.Async;
using Speckle.ConnectorRevit.UI;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using DB = Autodesk.Revit.DB;

namespace ConverterRevitTests
{
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
      ConverterRevit converter = new ConverterRevit();
      converter.SetContextDocument(fixture.SourceDoc);

      foreach (var elem in fixture.RevitElements)
      {
        await RevitTask.RunAsync(() =>
        {
          var spkElem = converter.ConvertToSpeckle(elem);
          if (spkElem is Base re)
            AssertValidSpeckleElement(elem, re);
        });
      }
      Assert.Equal(0, converter.Report.ConversionErrorsCount);
    }

    internal void NativeToSpeckleBase()
    {
      ConverterRevit kit = new ConverterRevit();
      kit.SetContextDocument(fixture.SourceDoc);

      foreach (var elem in fixture.RevitElements)
      {
        var spkElem = kit.ConvertToSpeckle(elem);
        Assert.NotNull(spkElem);
      }

      Assert.Equal(0, kit.Report.ConversionErrorsCount);
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
              throw e;
            }
          }
        }

        foreach (var appObj in appPlaceholders)
        {
          if (!(appObj.Converted.FirstOrDefault() is DB.Element element))
            continue;

          var testNumber = SpeckleUtils.GetSpeckleObjectTestNumber(element);
          if (testNumber == 0)
            continue;

          if (updateElementTestNumberMap.TryGetValue(testNumber, out var toNativeElementId))
            appObj.applicationId = toNativeElementId;
        }
      }

      ConverterRevit converter = new ConverterRevit();
      converter.SetContextDocument(doc);
      //setting context objects for nested routine
      var contextObjects = elements
        .Select(obj => new ApplicationObject(obj.UniqueId, obj.GetType().ToString()) { applicationId = obj.UniqueId })
        .ToList();
      converter.SetContextObjects(contextObjects);

      var spkElems = new List<Base>();
      await RevitTask
        .RunAsync(() =>
        {
          foreach (var elem in elements)
          {
            bool isAlreadyConverted = ConnectorBindingsRevit.GetOrCreateApplicationObject(
              elem,
              converter.Report,
              out ApplicationObject reportObj
            );
            if (isAlreadyConverted)
              continue;

            Base conversionResult = null;
            try
            {
              conversionResult = converter.ConvertToSpeckle(elem);
            }
            catch { }
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
      //setting context objects for update routine
      if (appPlaceholders != null)
        converter.SetPreviousContextObjects(appPlaceholders);

      converter.SetContextObjects(
        spkElems.Select(x => new ApplicationObject(x.id, x.speckle_type) { applicationId = x.applicationId }).ToList()
      );

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
            catch (Exception e)
            {
              converter.Report.LogConversionError(new Exception(e.Message, e));
            }

            if (res is List<ApplicationObject> apls)
            {
              resEls.AddRange(apls);
              flatSpkElems.Add(el);
              if (el["elements"] == null)
                continue;
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
      ConverterRevit converter = new ConverterRevit();
      converter.SetContextDocument(fixture.SourceDoc);
      var spkElems = fixture.Selection.Select(x => converter.ConvertToSpeckle(x) as Base).ToList();

      converter = new ConverterRevit();
      converter.SetContextDocument(fixture.NewDoc);
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
              revitEls.AddRange(apls);
            else
              revitEls.Add(res);
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

    internal void AssertValidSpeckleElement(DB.Element elem, Base spkElem)
    {
      Assert.NotNull(elem);
      Assert.NotNull(spkElem);
      Assert.NotNull(spkElem["speckle_type"]);
      Assert.NotNull(spkElem["applicationId"]);

      SpeckleUtils.CustomAssertions(elem, spkElem);
    }

    internal void AssertElementEqual(DB.Element sourceElem, DB.Element destElem)
    {
      Assert.NotNull(sourceElem);
      Assert.NotNull(destElem);
      Assert.Equal(sourceElem.Name, destElem.Name);
      Assert.Equal(sourceElem.GetTypeId(), destElem.GetTypeId());
      Assert.Equal(sourceElem.Category.Name, destElem.Category.Name);
    }

    internal void AssertFamilyInstanceEqual(DB.FamilyInstance sourceElem, DB.FamilyInstance destElem)
    {
      AssertElementEqual(sourceElem, destElem);

      AssertEqualParam(sourceElem, destElem, BuiltInParameter.FAMILY_BASE_LEVEL_PARAM);
      AssertEqualParam(sourceElem, destElem, BuiltInParameter.FAMILY_TOP_LEVEL_PARAM);
      AssertEqualParam(sourceElem, destElem, BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM);
      AssertEqualParam(sourceElem, destElem, BuiltInParameter.FAMILY_TOP_LEVEL_OFFSET_PARAM);

      AssertEqualParam(sourceElem, destElem, BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM);

      Assert.Equal(sourceElem.FacingFlipped, destElem.FacingFlipped);
      Assert.Equal(sourceElem.HandFlipped, destElem.HandFlipped);
      Assert.Equal(sourceElem.IsSlantedColumn, destElem.IsSlantedColumn);
      Assert.Equal(sourceElem.StructuralType, destElem.StructuralType);

      //rotation
      if (sourceElem.Location is LocationPoint)
        Assert.Equal(((LocationPoint)sourceElem.Location).Rotation, ((LocationPoint)destElem.Location).Rotation);
    }

    internal void AssertEqualParam(DB.Element expected, DB.Element actual, BuiltInParameter param)
    {
      var expecedParam = expected.get_Parameter(param);
      if (expecedParam == null)
        return;

      switch (expecedParam.StorageType)
      {
        case StorageType.Double:
          Assert.Equal(expecedParam.AsDouble(), actual.get_Parameter(param).AsDouble(), 4);
          break;
        case StorageType.Integer:
          Assert.Equal(expecedParam.AsInteger(), actual.get_Parameter(param).AsInteger());
          break;
        case StorageType.String:
          Assert.Equal(expecedParam.AsString(), actual.get_Parameter(param).AsString());
          break;
        case StorageType.ElementId:
        {
          var e1 = fixture.SourceDoc.GetElement(expecedParam.AsElementId());
          var e2 = fixture.NewDoc.GetElement(actual.get_Parameter(param).AsElementId());
          if (e1 is Level l1 && e2 is Level l2)
            Assert.Equal(l1.Elevation, l2.Elevation, 3);
          else if (e1 != null && e2 != null)
            Assert.Equal(e1.Name, e2.Name);
          break;
        }
        case StorageType.None:
          break;
        default:
          throw new ArgumentOutOfRangeException();
      }
    }
  }

  public class UpdateData
  {
    public Document Doc { get; set; }
    public IList<Element> Elements { get; set; }
    public List<ApplicationObject> AppPlaceholders { get; set; }
  }
}
