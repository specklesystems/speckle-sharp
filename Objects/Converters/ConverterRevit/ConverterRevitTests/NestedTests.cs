using System;
using Autodesk.Revit.DB;
using DB = Autodesk.Revit.DB;
using System.Collections.Generic;

using Xunit;
using Objects.Converter.Revit;
using System.Linq;
using xUnitRevitUtils;
using Speckle.Core.Models;

namespace ConverterRevitTests
{
  public class NestedFixture : SpeckleConversionFixture
  {
    public override string TestFile => Globals.GetTestModel("Nested.rvt");
    public override string NewFile => Globals.GetTestModel("Nested_ToNative.rvt");
    public override List<BuiltInCategory> Categories => new List<BuiltInCategory> {
      BuiltInCategory.OST_Doors,
      BuiltInCategory.OST_Walls,
      BuiltInCategory.OST_Windows,
      BuiltInCategory.OST_CeilingOpening,
      BuiltInCategory.OST_ColumnOpening,
      BuiltInCategory.OST_FloorOpening,
      BuiltInCategory.OST_ShaftOpening,
      BuiltInCategory.OST_StructuralFramingOpening,
      BuiltInCategory.OST_SWallRectOpening,
      BuiltInCategory.OST_ArcWallRectOpening,
      BuiltInCategory.OST_FloorOpening,
      BuiltInCategory.OST_SWallRectOpening,
      BuiltInCategory.OST_Floors};
    public NestedFixture() : base()
    {
    }
  }

  public class NestedTests : SpeckleConversionTest, IClassFixture<NestedFixture>
  {
    public NestedTests(NestedFixture fixture)
    {
      this.fixture = fixture;
    }

    //[Fact]
    //[Trait("Nested", "NestedToSpeckle")]
    //public void NestedToSpeckle()
    //{
    //  NativeToSpeckle();
    //}

    #region ToNative

    [Fact]
    [Trait("Nested", "ToNative")]
    public void NestedToNative()
    {

      ConverterRevit converter = new ConverterRevit();
      converter.SetContextDocument(fixture.SourceDoc);
      converter.SetContextObjects(fixture.RevitElements.Select(obj => new ApplicationPlaceholderObject { applicationId = obj.UniqueId }).ToList());
      var spkElems = converter.ConvertToSpeckle(fixture.RevitElements.Select(x => (object)x).ToList()).Where(x => x != null).ToList();

      converter = new ConverterRevit();
      converter.SetContextDocument(fixture.NewDoc);
      var resEls = new List<object>();
      var flatSpkElems = new List<Base>();

      xru.RunInTransaction(() =>
      {
        foreach (var el in spkElems)
        {
          var res = converter.ConvertToNative(el);
          if (res is List<ApplicationPlaceholderObject> apls)
          {
            resEls.AddRange(apls);
            flatSpkElems.Add(el);
            if (el["elements"] != null)
              flatSpkElems.AddRange(el["elements"] as List<Base>);
          }
          else
          {
            resEls.Add(el);
            flatSpkElems.Add(el);
          }
        }
      }, fixture.NewDoc).Wait();



      Assert.Empty(converter.ConversionErrors);

      for (var i = 0; i < resEls.Count; i++)
      {
        var correspondingSpk = flatSpkElems[i];
        var sourceElem = fixture.RevitElements.FirstOrDefault(x => x.UniqueId == correspondingSpk.applicationId);
        var destElement = ((ApplicationPlaceholderObject)resEls[i]).NativeObject as DB.Element;
        // T destElement;
        // if (resEls[i] is ApplicationPlaceholderObject apl) destElement = (T)apl.NativeObject;
        // else destElement = (T)resEls[i];

        AssertNestedEqual(sourceElem, destElement);
      }

      //for (var i = 0; i < revitEls.Count; i++)
      //{
      //  var sourceElem = fixture.RevitElements[i];
      //  var destElem = revitEls[i];
      //  AssertNestedEqual(sourceElem, destElem);
      //}
    }

    //[Fact]
    //[Trait("Nested", "NestedSelection")]
    //public void NestedSelectionToNative()
    //{
    //  SelectionToNative<DB.Element>(AssertNestedEqual);
    //}

    internal void AssertNestedEqual(DB.Element sourceElem, DB.Element destElem)
    {
      Assert.NotNull(destElem);
      Assert.Equal(sourceElem.Name, destElem.Name);

      //family instance
      AssertEqualParam(sourceElem, destElem, BuiltInParameter.FAMILY_BASE_LEVEL_PARAM);
      AssertEqualParam(sourceElem, destElem, BuiltInParameter.FAMILY_TOP_LEVEL_PARAM);
      AssertEqualParam(sourceElem, destElem, BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM);
      AssertEqualParam(sourceElem, destElem, BuiltInParameter.FAMILY_TOP_LEVEL_OFFSET_PARAM);
      AssertEqualParam(sourceElem, destElem, BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM);

      //rotation
      //for some reasons, rotation of hosted families stopped working in 2021.1 ...?
      if (sourceElem.Location is LocationPoint && sourceElem is FamilyInstance fi && fi.Host == null)
        Assert.Equal(((LocationPoint)sourceElem.Location).Rotation, ((LocationPoint)destElem.Location).Rotation);


      //walls
      AssertEqualParam(sourceElem, destElem, BuiltInParameter.WALL_USER_HEIGHT_PARAM);
      AssertEqualParam(sourceElem, destElem, BuiltInParameter.WALL_BASE_OFFSET);
      AssertEqualParam(sourceElem, destElem, BuiltInParameter.WALL_TOP_OFFSET);
      AssertEqualParam(sourceElem, destElem, BuiltInParameter.WALL_BASE_CONSTRAINT);
      AssertEqualParam(sourceElem, destElem, BuiltInParameter.WALL_HEIGHT_TYPE);
      AssertEqualParam(sourceElem, destElem, BuiltInParameter.WALL_STRUCTURAL_SIGNIFICANT);

    }

    #endregion

  }
}
