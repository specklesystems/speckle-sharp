using Objects;
using Autodesk.Revit.DB;
using DB = Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Text;
using Wall = Objects.Wall;
using Level = Objects.Level;
using Objects.Geometry;
using Mesh = Objects.Geometry.Mesh;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    // TODO: (OLD)  A polycurve spawning multiple walls is not yet handled properly with diffing, etc.
    // TODO: (OLD)  Most probably, just get rid of the polyline wall handling stuff. It's rather annyoing and confusing...
    public DB.Wall WallToNative(Wall speckleWall)
    {
      DB.Wall revitWall = null;

      var (docObj, stateObj) = GetExistingElementByApplicationId(speckleWall.applicationId, speckleWall.speckle_type);

      var wallType = GetElementByName(typeof(WallType), speckleWall.type) as WallType;
      var baseCurve = CurveToNative(speckleWall.baseGeometry as ICurve).get_Item(0); //TODO: support poliline/polycurve walls
      var structural = speckleWall.GetMemberSafe<bool>("structural");
      var level = LevelToNative(EnsureLevelExists(speckleWall.level, baseCurve));
      DB.Level topLevel = null;
      if (speckleWall.HasMember<Level>("topLevel"))
        topLevel = LevelToNative(speckleWall["topLevel"] as Level);
      var flipped = speckleWall.GetMemberSafe<bool>("flipped");

      //try update existing wall
      if (docObj != null)
      {
        try
        {
          revitWall = (DB.Wall)docObj;
          TrySetParam(revitWall, BuiltInParameter.WALL_BASE_CONSTRAINT, level);
          ((LocationCurve)revitWall.Location).Curve = baseCurve;
          if (revitWall.WallType.Name != wallType.Name)
            revitWall.ChangeTypeId(wallType.Id);
        }
        catch (Exception e)
        {
          //wall update failed, create a new one
        }
      }

      // create new wall
      if (revitWall == null)
      {
        revitWall = DB.Wall.Create(Doc, baseCurve, level.Id, structural);
      }

      TrySetParam(revitWall, BuiltInParameter.WALL_HEIGHT_TYPE, topLevel);

      revitWall.WallType = wallType as WallType;

      if (flipped != revitWall.Flipped)
        revitWall.Flip();

      SetElementParams(revitWall, speckleWall);

      return revitWall;
    }

    public Wall WallToSpeckle(DB.Wall revitWall)
    {
      //REVIT PARAMS > SPECKLE PROPS
      var heightParam = revitWall.get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM);
      //var baseOffsetParam = revitWall.get_Parameter(BuiltInParameter.WALL_BASE_OFFSET);
      //var topOffsetParam = revitWall.get_Parameter(BuiltInParameter.WALL_TOP_OFFSET);
      var baseLevelParam = revitWall.get_Parameter(BuiltInParameter.WALL_BASE_CONSTRAINT);
      var topLevelParam = revitWall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE);
      var structural = revitWall.get_Parameter(BuiltInParameter.WALL_STRUCTURAL_SIGNIFICANT); ;


      // SPECKLE WALL
      var speckleWall = new Wall();
      speckleWall.type = revitWall.WallType.Name;
      speckleWall.baseGeometry = LocationToSpeckle(revitWall);
      speckleWall.height = (double)ParameterToSpeckle(heightParam);
      speckleWall.level = (Level)ParameterToSpeckle(baseLevelParam);
      speckleWall["topLevel"] = (Level)ParameterToSpeckle(topLevelParam); //TODO: check if it works
      //speckleWall["baseOffset"] = (double)ParameterToSpeckle(baseOffsetParam);
      //speckleWall["topOffset"] = (double)ParameterToSpeckle(topOffsetParam);
      speckleWall["flipped"] = revitWall.Flipped;
      speckleWall["structural"] = (bool)ParameterToSpeckle(structural);
      speckleWall.displayMesh = GetWallDisplayMesh(revitWall);

      AddCommonRevitProps(speckleWall, revitWall);

      return speckleWall;
    }


    private Mesh GetWallDisplayMesh(DB.Wall wall)
    {
      var grid = wall.CurtainGrid;
      var mesh = new Mesh();

      // meshing for walls in case they are curtain grids
      if (grid != null)
      {
        var mySolids = new List<Solid>();
        foreach (ElementId panelId in grid.GetPanelIds())
        {
          mySolids.AddRange(MeshUtils.GetElementSolids(Doc.GetElement(panelId)));
        }
        foreach (ElementId mullionId in grid.GetMullionIds())
        {
          mySolids.AddRange(MeshUtils.GetElementSolids(Doc.GetElement(mullionId)));
        }
        (mesh.faces, mesh.vertices) = MeshUtils.GetFaceVertexArrFromSolids(mySolids, Scale);
      }
      else
      {
        (mesh.faces, mesh.vertices) = MeshUtils.GetFaceVertexArrayFromElement(wall, Scale, new Options() { DetailLevel = ViewDetailLevel.Fine, ComputeReferences = false });

      }

      return mesh;
    }
  }
}
