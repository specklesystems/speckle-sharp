using System;
using Autodesk.Revit.DB;
using DB = Autodesk.Revit.DB;
using Objects;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Element = Objects.Element;
using xUnitRevitUtils;
using System.CodeDom;
using Objects.Converter.Revit;
using Speckle.Core.Models;

namespace ConverterRevitTests
{
  public class SpeckleConversionTest
  {

    internal SpeckleConversionFixture fixture;


    internal void NativeToSpeckle()
    {
      ConverterRevit kit = new ConverterRevit();

      foreach (var elem in fixture.RevitElements)
      {
        var spkElem = kit.ConvertToSpeckle(elem) as Element;
        AssertValidSpeckleElement(elem, spkElem);
      }
      Assert.Empty(kit.ConversionErrors);
    }

    internal void NativeToSpeckleBase()
    {
      ConverterRevit kit = new ConverterRevit();

      foreach (var elem in fixture.RevitElements)
      {
        var spkElem = kit.ConvertToSpeckle(elem);
        Assert.NotNull(spkElem);
      }
      Assert.Empty(kit.ConversionErrors);
    }

    internal void SpeckleToNative<T>(Action<T, T> assert)
    {
      ConverterRevit kit = new ConverterRevit();
      var spkElems = fixture.RevitElements.Select(x => kit.ConvertToSpeckle(x) as Base).ToList();

      kit = new ConverterRevit();
      var revitEls = new List<T>();

      xru.RunInTransaction(() =>
      {
        revitEls = spkElems.Select(x => (T)kit.ConvertToNative(x)).ToList();
      }, fixture.NewDoc).Wait();

      Assert.Empty(kit.ConversionErrors);

      for (var i = 0; i < revitEls.Count; i++)
      {
        var sourceElem = (T)(object)fixture.RevitElements[i];
        var destElem = revitEls[i];
        assert(sourceElem, destElem);
      }
    }

    internal void SelectionToNative<T>(Action<T, T> assert)
    {
      ConverterRevit kit = new ConverterRevit();
      var spkElems = fixture.Selection.Select(x => kit.ConvertToSpeckle(x) as Base).ToList();

      kit = new ConverterRevit();
      var revitEls = new List<T>();

      xru.RunInTransaction(() =>
      {
        revitEls = spkElems.Select(x => (T)kit.ConvertToNative(x)).ToList();
      }, fixture.NewDoc).Wait();

      Assert.Empty(kit.ConversionErrors);

      for (var i = 0; i < revitEls.Count; i++)
      {
        var sourceElem = (T)(object)fixture.Selection[i];
        var destElem = revitEls[i];
        assert(sourceElem, destElem);
      }
    }

    internal void AssertValidSpeckleElement(DB.Element elem, Element spkElem)
    {
      Assert.NotNull(elem);
      Assert.NotNull(spkElem);
      if (!(elem is DB.Architecture.Room || elem is DB.Mechanical.Duct))
        Assert.Equal(elem.Name, spkElem.type);
      Assert.NotNull(spkElem["parameters"]);
      Assert.NotNull(spkElem.baseGeometry);
      //Assert.NotNull(spkElem.level);
      Assert.NotNull(spkElem.displayMesh);
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

      if (expecedParam.StorageType == StorageType.Double)
      {
        Assert.Equal(expecedParam.AsDouble(), actual.get_Parameter(param).AsDouble(), 4);
      }
      else if (expecedParam.StorageType == StorageType.Integer)
      {
        Assert.Equal(expecedParam.AsInteger(), actual.get_Parameter(param).AsInteger());
      }
      else if (expecedParam.StorageType == StorageType.String)
      {
        Assert.Equal(expecedParam.AsString(), actual.get_Parameter(param).AsString());
      }
      else if (expecedParam.StorageType == StorageType.ElementId)
      {
        var e1 = fixture.Doc.GetElement(expecedParam.AsElementId());
        var e2 = fixture.NewDoc.GetElement(actual.get_Parameter(param).AsElementId());
        if (e1 != null && e2 != null)
          Assert.Equal(e1.Name, e2.Name);
      }
    }


  }
}
