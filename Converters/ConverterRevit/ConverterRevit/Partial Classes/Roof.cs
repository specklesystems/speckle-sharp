
using Autodesk.Revit.DB;
using DB = Autodesk.Revit.DB;
using Roof = Objects.Roof;
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
using Objects.Revit;
using Line = Objects.Geometry.Line;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public DB.Element RoofToNative(Roof speckleRoof)
    {
      DB.RoofBase revitRoof = null;
      var (docObj, stateObj) = GetExistingElementByApplicationId(speckleRoof.applicationId, speckleRoof.type);


      var roofType = GetElementByName(typeof(RoofType), speckleRoof.type) as RoofType;
      var outline = CurveToNative(speckleRoof.baseGeometry as ICurve);
      var level = LevelToNative(EnsureLevelExists(speckleRoof.level, outline));

      // NOTE: I have not found a way to edit a slab outline properly, so whenever we bake, we renew the element.
      if (docObj != null)
        Doc.Delete(docObj.Id);

      if (speckleRoof is RevitExtrusionRoof)
      {

        var speckleExtrusionRoof = speckleRoof as RevitExtrusionRoof;

        var referenceLine = LineToNative(speckleExtrusionRoof.referenceLine);

        var norm = GetPerpendicular(referenceLine.GetEndPoint(0) - referenceLine.GetEndPoint(1)).Negate();

        ReferencePlane plane = Doc.Create.NewReferencePlane(referenceLine.GetEndPoint(0),
                                        referenceLine.GetEndPoint(1),
                                        norm,
                                        Doc.ActiveView);
        //create floor without a type
        revitRoof = Doc.Create.NewExtrusionRoof(outline, plane, level, roofType, speckleExtrusionRoof.start, speckleExtrusionRoof.end);
      }
      else if (speckleRoof is RevitFootprintRoof)
      {
        var speckleFootprintRoof = speckleRoof as RevitFootprintRoof;



        ModelCurveArray curveArray = new ModelCurveArray();
        var revitFootprintRoof = Doc.Create.NewFootPrintRoof(outline, level, roofType, out curveArray);
        for (var i = 0; i < curveArray.Size; i++)
        {
          var poly = speckleRoof.baseGeometry as Polycurve;
          revitFootprintRoof.set_DefinesSlope(curveArray.get_Item(i), ((Base)poly.segments[i]).GetMemberSafe<bool>("isSloped"));
          try
          {
            revitFootprintRoof.set_SlopeAngle(curveArray.get_Item(i), ((Base)poly.segments[i]).GetMemberSafe<double>("slopeAngle"));
          }
          catch { }
          revitFootprintRoof.set_Offset(curveArray.get_Item(i), ((Base)poly.segments[i]).GetMemberSafe<double>("offset"));


        }
        if (speckleFootprintRoof.cutOffLevel != null)
        {
          var cutOfflevel = LevelToNative(speckleFootprintRoof.cutOffLevel);
          TrySetParam(revitFootprintRoof, BuiltInParameter.ROOF_UPTO_LEVEL_PARAM, cutOfflevel);
        }

        revitRoof = revitFootprintRoof;
      }

      Doc.Regenerate();

      try
      {
        MakeOpeningsInRoof(revitRoof, speckleRoof.holes.ToList());
      }
      catch (Exception ex)
      {
        ConversionErrors.Add(new Error("Could not create holes in roof", ex.Message));
      }
      SetElementParams(revitRoof, speckleRoof);
      return revitRoof;


    }

    private void MakeOpeningsInRoof(DB.RoofBase roof, List<ICurve> holes)
    {
      foreach (var hole in holes)
      {
        var curveArray = CurveToNative(hole);
        Doc.Create.NewOpening(roof, curveArray, true);
      }
    }

    private Element RoofToSpeckle(DB.RoofBase revitRoof)
    {


      var profiles = GetProfiles(revitRoof);

      var speckleRoof = new Roof();

      //assigning correct type for when going back in Revit
      if (revitRoof is FootPrintRoof)
      {
        var baseLevelParam = revitRoof.get_Parameter(BuiltInParameter.ROOF_BASE_LEVEL_PARAM);
        var cutOffLevelParam = revitRoof.get_Parameter(BuiltInParameter.ROOF_UPTO_LEVEL_PARAM);
        var speckleFootprintRoof = new RevitFootprintRoof();
        speckleFootprintRoof.level = (Level)ParameterToSpeckle(baseLevelParam);
        speckleFootprintRoof.cutOffLevel = (Level)ParameterToSpeckle(cutOffLevelParam);

        speckleRoof = speckleFootprintRoof;
      }

      else if (revitRoof is ExtrusionRoof)
      {
        var revitExtrusionRoof = revitRoof as ExtrusionRoof;
        var speckleExtrusionRoof = new RevitExtrusionRoof();
        speckleExtrusionRoof.start = (double)ParameterToSpeckle(revitExtrusionRoof.get_Parameter(BuiltInParameter.EXTRUSION_START_PARAM));
        speckleExtrusionRoof.end = (double)ParameterToSpeckle(revitExtrusionRoof.get_Parameter(BuiltInParameter.EXTRUSION_END_PARAM));
        var plane = revitExtrusionRoof.GetProfile().get_Item(0).SketchPlane.GetPlane();
        speckleExtrusionRoof.referenceLine = new Line(PointToSpeckle(plane.Origin.Add(plane.XVec.Normalize().Negate())), PointToSpeckle(plane.Origin)); //TODO: test!
        var baseLevelParam = revitRoof.get_Parameter(BuiltInParameter.ROOF_CONSTRAINT_LEVEL_PARAM);
        speckleExtrusionRoof.level = (Level)ParameterToSpeckle(baseLevelParam);
        speckleRoof = speckleExtrusionRoof;
      }


      speckleRoof.type = Doc.GetElement(revitRoof.GetTypeId()).Name;

      // TODO handle case if not one of our supported roofs
      if ( profiles.Any() )
      {
        speckleRoof.baseGeometry = profiles[0];
        if (profiles.Count > 1)
          speckleRoof.holes = profiles.Skip(1).ToList();
      }


      AddCommonRevitProps(speckleRoof, revitRoof);

      (speckleRoof.displayMesh.faces, speckleRoof.displayMesh.vertices) = MeshUtils.GetFaceVertexArrayFromElement(revitRoof, Scale, new Options() { DetailLevel = ViewDetailLevel.Fine, ComputeReferences = false });
      return speckleRoof;
    }

    //Nesting the various profiles into a polycurve segments
    private List<ICurve> GetProfiles(DB.RoofBase roof)
    {
      var profiles = new List<ICurve>();

      if (roof is FootPrintRoof)
      {
        var footprint = roof as FootPrintRoof;
        ModelCurveArrArray crvLoops = footprint.GetProfiles();

        for (var i = 0; i < crvLoops.Size; i++)
        {
          var crvloop = crvLoops.get_Item(i);
          var poly = new Polycurve();
          foreach (DB.ModelCurve curve in crvloop)
          {
            if (curve == null) continue;

            var segment = CurveToSpeckle(curve.GeometryCurve) as Base; //it's a safe casting, should be improved tho...
            segment["slopeAngle"] = ParameterToSpeckle(curve.get_Parameter(BuiltInParameter.ROOF_SLOPE));
            segment["isSloped"] = ParameterToSpeckle(curve.get_Parameter(BuiltInParameter.ROOF_CURVE_IS_SLOPE_DEFINING));
            segment["offset"] = ParameterToSpeckle(curve.get_Parameter(BuiltInParameter.ROOF_CURVE_HEIGHT_OFFSET));
            poly.segments.Add(segment as ICurve);

            //roud profiles are returned duplicated!
            if (curve is ModelArc && (curve as ModelArc).GeometryCurve.IsClosed)
              break;
          }
          profiles.Add(poly);
        }

      }
      else if (roof is ExtrusionRoof)
      {
        var extrusion = roof as ExtrusionRoof;
        var crvloop = extrusion.GetProfile();
        var poly = new Polycurve();
        foreach (DB.ModelCurve curve in crvloop)
        {
          if (curve == null) continue;
          poly.segments.Add(CurveToSpeckle(curve.GeometryCurve));
        }
        profiles.Add(poly);
      }
      return profiles;
    }
  }
}
