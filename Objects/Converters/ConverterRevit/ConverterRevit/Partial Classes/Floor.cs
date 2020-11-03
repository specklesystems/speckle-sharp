
using Autodesk.Revit.DB;
using DB = Autodesk.Revit.DB;
using Floor = Objects.Floor;
using Element = Objects.Element;
using Level = Objects.Level;
using Autodesk.Revit.DB.Structure;
using Mesh = Objects.Geometry.Mesh;
using Objects.Geometry;
using System.Collections.Generic;
using System;
using System.Linq;
using Objects;
using Speckle.Core.Models;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public DB.Element FloorToNative(Floor speckleFloor)
    {
      DB.Floor revitFloor = null;
      var (docObj, stateObj) = GetExistingElementByApplicationId(speckleFloor.applicationId, speckleFloor.type);


      var floorType = GetElementByName(typeof(FloorType), speckleFloor.type) as FloorType;
      var outline = CurveToNative(speckleFloor.baseGeometry as ICurve);
      var level = LevelToNative(EnsureLevelExists(speckleFloor.level, outline));
      var structural = speckleFloor.GetMemberSafe<bool>("structural");

      // NOTE: I have not found a way to edit a slab outline properly, so whenever we bake, we renew the element.
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
        MakeOpeningsInFloor(revitFloor, speckleFloor.holes.ToList());
      }
      catch (Exception ex)
      {
        ConversionErrors.Add(new Error("Could not create holes in floor", ex.Message));
      }
      SetElementParams(revitFloor, speckleFloor);
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

    private Element FloorToSpeckle(DB.Floor revitFloor)
    {
      var baseLevelParam = revitFloor.get_Parameter(BuiltInParameter.LEVEL_PARAM);
      var structuralParam = revitFloor.get_Parameter(BuiltInParameter.FLOOR_PARAM_IS_STRUCTURAL);
      var profiles = GetProfiles(revitFloor);

      var speckleFloor = new Floor();
      speckleFloor.type = Doc.GetElement(revitFloor.GetTypeId()).Name;
      speckleFloor.baseGeometry = profiles[0];
      if (profiles.Count > 1)
        speckleFloor.holes = profiles.Skip(1).ToList();
      speckleFloor.level = (Level)ParameterToSpeckle(baseLevelParam);
      speckleFloor["structural"] = ParameterToSpeckle(structuralParam);

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
