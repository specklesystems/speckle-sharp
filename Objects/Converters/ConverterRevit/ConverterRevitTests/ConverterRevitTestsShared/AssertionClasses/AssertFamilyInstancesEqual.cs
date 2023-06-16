using System.Collections.Generic;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using ConverterRevitTests;
using Objects.Converter.Revit;
using Speckle.Core.Models;
using Xunit;
using DB = Autodesk.Revit.DB;

namespace ConverterRevitTestsShared.AssertionClasses
{
  internal class AssertFamilyInstancesEqual
  {
    private Dictionary<string, ApplicationObject> _reportObjects;

    public AssertFamilyInstancesEqual(Dictionary<string, ApplicationObject> reportObjects)
    {
      _reportObjects = reportObjects;
    }

    public Task Handle(FamilyInstance sourceElement, FamilyInstance destElement, Base speckleElement)
    {
      AssertUtils.ElementEqual(sourceElement, destElement);
      ParamAssertions(sourceElement, destElement);

      if (sourceElement.Location is LocationPoint locationPoint1)
      {
        var locationPoint2 = (LocationPoint)destElement.Location;
        Assert.Equal(locationPoint1.Point.X, locationPoint2.Point.X, 2);
        Assert.Equal(locationPoint1.Point.Y, locationPoint2.Point.Y, 2);
        Assert.Equal(locationPoint1.Point.Z, locationPoint2.Point.Z, 2);
      }

      FacingAndHandAssertions(sourceElement, destElement, speckleElement);

      if (sourceElement.Host == null)
      {
        return Task.CompletedTask;
      }

      // workplane based elements can be tough to receive with the correct host
      if (sourceElement.Symbol.Family.FamilyPlacementType != FamilyPlacementType.WorkPlaneBased)
      {
        Assert.Equal(sourceElement.Host.Name, destElement.Host.Name);
      }

      return Task.CompletedTask;
    }

    private static void ParamAssertions(FamilyInstance sourceElement, FamilyInstance destElement)
    {
      AssertUtils.EqualParam(sourceElement, destElement, BuiltInParameter.FAMILY_LEVEL_PARAM);
      AssertUtils.EqualParam(sourceElement, destElement, BuiltInParameter.INSTANCE_ELEVATION_PARAM);
      AssertUtils.EqualParam(sourceElement, destElement, BuiltInParameter.INSTANCE_SILL_HEIGHT_PARAM);

      AssertUtils.EqualParam(sourceElement, destElement, BuiltInParameter.FAMILY_BASE_LEVEL_PARAM);
      AssertUtils.EqualParam(sourceElement, destElement, BuiltInParameter.FAMILY_TOP_LEVEL_PARAM);
      AssertUtils.EqualParam(sourceElement, destElement, BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM);
      AssertUtils.EqualParam(sourceElement, destElement, BuiltInParameter.FAMILY_TOP_LEVEL_OFFSET_PARAM);
      AssertUtils.EqualParam(sourceElement, destElement, BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM);
    }

    private void FacingAndHandAssertions(FamilyInstance sourceElement, FamilyInstance destElement, Base speckleElement)
    {
      var destFacingOrientation = destElement.FacingOrientation;
      var destHandOrientation = destElement.HandOrientation;

      if (_reportObjects[speckleElement.id].Log.Contains(ConverterRevit.faceFlipFailErrorMsg))
      {
        Assert.Equal(sourceElement.FacingFlipped, !destElement.FacingFlipped);
        destFacingOrientation *= -1;
      }
      else
      {
        Assert.Equal(sourceElement.FacingFlipped, destElement.FacingFlipped);
      }

      if (_reportObjects[speckleElement.id].Log.Contains(ConverterRevit.handFlipFailErrorMsg))
      {
        Assert.Equal(sourceElement.HandFlipped, !destElement.HandFlipped);
        destHandOrientation *= -1;
      }
      else
      {
        Assert.Equal(sourceElement.HandFlipped, destElement.HandFlipped);
      }

      Assert.Equal(sourceElement.FacingOrientation.X, destFacingOrientation.X, 2);
      Assert.Equal(sourceElement.FacingOrientation.Y, destFacingOrientation.Y, 2);
      Assert.Equal(sourceElement.FacingOrientation.Z, destFacingOrientation.Z, 2);

      Assert.Equal(sourceElement.HandOrientation.X, destHandOrientation.X, 2);
      Assert.Equal(sourceElement.HandOrientation.Y, destHandOrientation.Y, 2);
      Assert.Equal(sourceElement.HandOrientation.Z, destHandOrientation.Z, 2);
    }
  }
}
