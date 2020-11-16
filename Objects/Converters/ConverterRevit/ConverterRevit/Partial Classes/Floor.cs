
using Autodesk.Revit.DB;
using Objects.Geometry;
using Objects.Revit;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using DB = Autodesk.Revit.DB;
using Element = Objects.BuiltElements.Element;
using Floor = Objects.BuiltElements.Floor;
using Level = Objects.BuiltElements.Level;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public DB.Element FloorToNative(Floor speckleFloor)
    {
      if (speckleFloor.outline == null)
      {
        throw new Exception("Only outline based Floor are currently supported.");
      }

      DB.Level level = null;
      DB.Floor revitFloor = null;
      bool structural = false;
      var outline = CurveToNative(speckleFloor.outline);
      var type = "";


      var speckleRevitFloor = speckleFloor as RevitFloor;
      if (speckleRevitFloor != null)
      {
        level = LevelToNative(speckleRevitFloor.level);
        structural = speckleRevitFloor.structural;
        type = speckleRevitFloor.type;
      }
      else
      {
        level = LevelToNative(LevelFromCurve(outline.get_Item(0)));
      }

      var floorType = GetElementByTypeAndName<FloorType>(type);

      // NOTE: I have not found a way to edit a slab outline properly, so whenever we bake, we renew the element.
      var (docObj, stateObj) = GetExistingElementByApplicationId(speckleFloor.applicationId, speckleFloor.speckle_type);
      if (docObj != null)
        Doc.Delete(docObj.Id);

      if (floorType == null)
      {
        //create floor without a type
        revitFloor = Doc.Create.NewFloor(outline, structural);
      }
      else
      {
        revitFloor = Doc.Create.NewFloor(outline, floorType, level, structural);
      }

      Doc.Regenerate();

      try
      {
        MakeOpeningsInFloor(revitFloor, speckleFloor.voids.ToList());
      }
      catch (Exception ex)
      {
        ConversionErrors.Add(new Error("Could not create holes in floor", ex.Message));
      }
      if (speckleRevitFloor != null)
      {
        SetElementParams(revitFloor, speckleRevitFloor);
      }
      return revitFloor;


    }

    private void MakeOpeningsInFloor(DB.Floor floor, List<ICurve> holes)
    {
      foreach (var hole in holes)
      {
        var curveArray = CurveToNative(hole);
        Doc.Create.NewOpening(floor, curveArray, true);
      }
    }

    private IRevit FloorToSpeckle(DB.Floor revitFloor)
    {
      var baseLevelParam = revitFloor.get_Parameter(BuiltInParameter.LEVEL_PARAM);
      var structuralParam = revitFloor.get_Parameter(BuiltInParameter.FLOOR_PARAM_IS_STRUCTURAL);
      var profiles = GetProfiles(revitFloor);

      var speckleFloor = new RevitFloor();
      speckleFloor.type = Doc.GetElement(revitFloor.GetTypeId()).Name;
      speckleFloor.outline = profiles[0];
      if (profiles.Count > 1)
        speckleFloor.voids = profiles.Skip(1).ToList();
      speckleFloor.level = (RevitLevel)ParameterToSpeckle(baseLevelParam);
      speckleFloor.structural = (bool)ParameterToSpeckle(structuralParam);

      AddCommonRevitProps(speckleFloor, revitFloor);

      (speckleFloor.displayMesh.faces, speckleFloor.displayMesh.vertices) = MeshUtils.GetFaceVertexArrayFromElement(revitFloor, Scale, new Options() { DetailLevel = ViewDetailLevel.Fine, ComputeReferences = false });
      return speckleFloor;
    }

    //Nesting the various profiles into a polycurve segments
    private List<ICurve> GetProfiles(DB.CeilingAndFloor floor)
    {
      var profiles = new List<ICurve>();
      var faces = HostObjectUtils.GetTopFaces(floor);
      Face face = floor.GetGeometryObjectFromReference(faces[0]) as Face;
      var crvLoops = face.GetEdgesAsCurveLoops();
      foreach (var crvloop in crvLoops)
      {
        var poly = new Polycurve();
        foreach (var curve in crvloop)
        {
          var c = curve as DB.Curve;

          if (c == null) continue;
          poly.segments.Add(CurveToSpeckle(c));
        }
        profiles.Add(poly);
      }
      return profiles;
    }
  }
}
