using Autodesk.Revit.DB;
using Objects.Converter.Revit;
using Revit.Async;
using Speckle.Core.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using xUnitRevitUtils;

using DB = Autodesk.Revit.DB;
using DirectShape = Objects.BuiltElements.Revit.DirectShape;

namespace ConverterRevitTests
{
  public class SpeckleConversionTest
  {
    internal SpeckleConversionFixture fixture;

    internal void NativeToSpeckle()
    {
      ConverterRevit converter = new ConverterRevit();
      converter.SetContextDocument(fixture.SourceDoc);

      foreach (var elem in fixture.RevitElements)
      {
        var spkElem = converter.ConvertToSpeckle(elem);
        if (spkElem is Base re)
          AssertValidSpeckleElement(elem, re);
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
    internal async Task<List<object>> SpeckleToNative<T>(Action<T, T> assert, UpdateData ud = null)
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
      }


      ConverterRevit converter = new ConverterRevit();
      converter.SetContextDocument(doc);
      //setting context objects for nested routine
      converter.SetContextObjects(elements.Select(obj => new ApplicationObject (obj.UniqueId, obj.GetType().ToString()) { applicationId = obj.UniqueId }).ToList());


      var spkElems = new List<Base>();
      await RevitTask.RunAsync(() =>
      {
        spkElems = elements.Select(x => converter.ConvertToSpeckle(x)).Where(x => x != null).ToList();
      });

      converter = new ConverterRevit();
      converter.ReceiveMode = Speckle.Core.Kits.ReceiveMode.Update;

      converter.SetContextDocument(fixture.NewDoc);
      //setting context objects for update routine
      if (appPlaceholders != null)
        converter.SetPreviousContextObjects(appPlaceholders);

      converter.SetContextObjects(spkElems.Select(x => new ApplicationObject(x.id, x.speckle_type) { applicationId = x.applicationId}).ToList());


      var resEls = new List<object>();
      //used to associate th nested Base objects with eh flat revit ones
      var flatSpkElems = new List<Base>();

      await RunInTransaction(() =>
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
            if (el["elements"] == null) continue;
            flatSpkElems.AddRange((el["elements"] as List<Base>).Where(b => converter.CanConvertToNative(b)));
          }
          else
          {
            resEls.Add(res);
            flatSpkElems.Add(el);
          }
        }
        //}, fixture.NewDoc).Wait();
      }, fixture.NewDoc, converter);

      Assert.Equal(0, converter.Report.ConversionErrorsCount);

      for (var i = 0; i < spkElems.Count; i++)
      {
        var sourceElem = (T)(object)elements.FirstOrDefault(x => x.UniqueId == flatSpkElems[i].applicationId);
        var destElement = (T)((ApplicationObject)resEls[i]).Converted.FirstOrDefault();
        assert(sourceElem, destElement);
        if (!fixture.UpdateTestRunning)
          DeleteElement(destElement);
      }

      return resEls;
    }

    /// <summary>
    /// Runs SpeckleToNative with SourceDoc and UpdatedDoc
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="assert"></param>
    internal async Task SpeckleToNativeUpdates<T>(Action<T, T> assert)
    {
      fixture.UpdateTestRunning = true;
      var initialObjs = await SpeckleToNative(assert);
      var updatedObjs = await SpeckleToNative(assert, new UpdateData
      {
        AppPlaceholders = initialObjs.Cast<ApplicationObject>().ToList(),
        Doc = fixture.UpdatedDoc,
        Elements = fixture.UpdatedRevitElements
      });
      fixture.UpdateTestRunning = false;

      // delete the elements that were not being deleted during the update test
      DeleteElement(initialObjs);
      //DeleteElement(updatedObjs);
    }

    internal async Task SelectionToNative<T>(Action<T, T> assert)
    {
      ConverterRevit converter = new ConverterRevit();
      converter.SetContextDocument(fixture.SourceDoc);
      var spkElems = fixture.Selection.Select(x => converter.ConvertToSpeckle(x) as Base).ToList();

      converter = new ConverterRevit();
      converter.SetContextDocument(fixture.NewDoc);
      var revitEls = new List<object>();
      var resEls = new List<object>();

      await RunInTransaction(() =>
      {
        //xru.RunInTransaction(() =>
        //{
        foreach (var el in spkElems)
        {
          var res = converter.ConvertToNative(el);
          if (res is List<ApplicationObject> apls)
            resEls.AddRange(apls);
          else
            resEls.Add(el);
        }
        //}, fixture.NewDoc).Wait();
      }, fixture.NewDoc, converter);

      Assert.Equal(0, converter.Report.ConversionErrorsCount);

      for (var i = 0; i < revitEls.Count; i++)
      {
        var sourceElem = (T)(object)fixture.RevitElements[i];
        var destElement = (T)((ApplicationObject)resEls[i]).Converted.FirstOrDefault();
        assert(sourceElem, (T)destElement);
        if (!fixture.UpdateTestRunning)
          DeleteElement(destElement);
      }
    }

    internal class IgnoreAllWarnings : Autodesk.Revit.DB.IFailuresPreprocessor
    {
      public FailureProcessingResult PreprocessFailures(Autodesk.Revit.DB.FailuresAccessor failuresAccessor)
      {
        IList<Autodesk.Revit.DB.FailureMessageAccessor> failureMessages = failuresAccessor.GetFailureMessages();
        foreach (Autodesk.Revit.DB.FailureMessageAccessor item in failureMessages)
        {
          failuresAccessor.DeleteWarning(item);
        }

        return FailureProcessingResult.Continue;
      }
    }

    internal async static Task<string> RunInTransaction(Action action, Document doc, ConverterRevit converter, string transactionName = "transaction", bool ignoreWarnings = false)
    {
      var tcs = new TaskCompletionSource<string>();

      await RevitTask.RunAsync(() =>
      {
        using var g = new TransactionGroup(doc, transactionName);
        using var transaction = new Transaction(doc, transactionName);

        g.Start();
        transaction.Start();

        converter.SetContextDocument(transaction);

        if (ignoreWarnings)
        {
          var options = transaction.GetFailureHandlingOptions();
          options.SetFailuresPreprocessor(new IgnoreAllWarnings());
          transaction.SetFailureHandlingOptions(options);
        }

        try
        {
          action.Invoke();
          transaction.Commit();
          g.Assimilate();
        }
        catch (Exception exception)
        {
          tcs.TrySetException(exception);
        }

        tcs.TrySetResult("");
      });

      return await tcs.Task;
    }

    internal void DeleteElement(object obj)
    {
      switch (obj)
      {
        case IList list:
          foreach (var item in list)
            DeleteElement(item);
          break;
        case ApplicationObject o:
          foreach (var item in o.Converted)
            DeleteElement(item);
          break;
        case DB.Element o:
          try
          {
            xru.RunInTransaction(() =>
            {
              o.Document.Delete(o.Id);
            }, o.Document).Wait();
          }
          // element already deleted, don't worry about it
          catch { }
          break;
        default:
          throw new Exception("It's not an element!?!?!");
      }
    }

    internal void AssertValidSpeckleElement(DB.Element elem, Base spkElem)
    {
      Assert.NotNull(elem);
      Assert.NotNull(spkElem);
      Assert.NotNull(spkElem["parameters"]);
      Assert.NotNull(spkElem["elementId"]);

      var elemAsFam = elem as FamilyInstance;
      // HACK: This is not reliable or acceptable as a testing strategy.
      if (!(elem is DB.Architecture.Room || elem is DB.Mechanical.Duct ||
            elemAsFam != null && AdaptiveComponentInstanceUtils.IsAdaptiveComponentInstance(elem as FamilyInstance) ))
        Assert.Equal(elem.Name, spkElem["type"]);

      //Assert.NotNull(spkElem.baseGeometry);

      //Assert.NotNull(spkElem.level);
      //Assert.NotNull(spkRevit.displayMesh);
    }

    internal void AssertFamilyInstanceEqual(DB.FamilyInstance sourceElem, DB.FamilyInstance destElem)
    {
      Assert.NotNull(destElem);
      Assert.Equal(sourceElem.Name, destElem.Name);

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
