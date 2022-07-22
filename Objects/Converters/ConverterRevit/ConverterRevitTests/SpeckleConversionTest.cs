using Autodesk.Revit.DB;
using Objects.Converter.Revit;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
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
    internal List<object> SpeckleToNative<T>(Action<T, T> assert, UpdateData ud = null)
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
      var spkElems = elements.Select(x => converter.ConvertToSpeckle(x)).Where(x => x != null).ToList();

      converter = new ConverterRevit();
      converter.SetContextDocument(fixture.NewDoc);
      //setting context objects for update routine
      if (appPlaceholders != null)
        converter.SetPreviousContextObjects(appPlaceholders);

      converter.SetContextObjects(spkElems.Select(x => new ApplicationObject(x.id, x.speckle_type) { applicationId = x.applicationId}).ToList());


      var resEls = new List<object>();
      //used to associate th nested Base objects with eh flat revit ones
      var flatSpkElems = new List<Base>();

      xru.RunInTransaction(() =>
      {
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
      }, fixture.NewDoc).Wait();

      Assert.Equal(0, converter.Report.ConversionErrorsCount);

      for (var i = 0; i < spkElems.Count; i++)
      {
        var sourceElem = (T)(object)elements.FirstOrDefault(x => x.UniqueId == flatSpkElems[i].applicationId);
        var destElement = (T)((ApplicationObject)resEls[i]).Converted.FirstOrDefault();
        assert(sourceElem, destElement);
      }

      return resEls;
    }

    /// <summary>
    /// Runs SpeckleToNative with SourceDoc and UpdatedDoc
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="assert"></param>
    internal void SpeckleToNativeUpdates<T>(Action<T, T> assert)
    {
      var result = SpeckleToNative(assert);

      SpeckleToNative(assert, new UpdateData
      {
        AppPlaceholders = result.Cast<ApplicationObject>().ToList(),
        Doc = fixture.UpdatedDoc,
        Elements = fixture.UpdatedRevitElements
      });
    }

    internal void SelectionToNative<T>(Action<T, T> assert)
    {
      ConverterRevit converter = new ConverterRevit();
      converter.SetContextDocument(fixture.SourceDoc);
      var spkElems = fixture.Selection.Select(x => converter.ConvertToSpeckle(x) as Base).ToList();

      converter = new ConverterRevit();
      converter.SetContextDocument(fixture.NewDoc);
      var revitEls = new List<object>();
      var resEls = new List<object>();

      xru.RunInTransaction(() =>
      {
        //revitEls = spkElems.Select(x => kit.ConvertToNative(x)).ToList();
        foreach (var el in spkElems)
        {
          var res = converter.ConvertToNative(el);
          if (res is List<ApplicationObject> apls)
            resEls.AddRange(apls);
          else 
            resEls.Add(el);
        }
      }, fixture.NewDoc).Wait();

      Assert.Equal(0, converter.Report.ConversionErrorsCount);

      for (var i = 0; i < revitEls.Count; i++)
      {
        var sourceElem = (T)(object)fixture.RevitElements[i];
        var destElement = (T)((ApplicationObject)resEls[i]).Converted.FirstOrDefault();

        assert(sourceElem, (T)destElement);
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