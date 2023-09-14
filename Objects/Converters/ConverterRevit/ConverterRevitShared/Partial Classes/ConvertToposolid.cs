using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Autodesk.Revit.DB;
using Objects.BuiltElements;
using Objects.BuiltElements.Revit;
using Objects.Geometry;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using DB = Autodesk.Revit.DB;
using OG = Objects.Geometry;
using OO = Objects.Other;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public ApplicationObject SpeckleToposolidToNative(BuiltElements.SpeckleToposolid speckleToposolid)
    {
      var docObj = GetExistingElementByApplicationId(speckleToposolid.applicationId);
      var appObj = new ApplicationObject(speckleToposolid.id, speckleToposolid.speckle_type)
      {
        applicationId = speckleToposolid.applicationId
      };

      // skip if element already exists in doc & receive mode is set to ignore
      if (IsIgnore(docObj, appObj))
        return appObj;

      // get the curves and the points
      var curveLoops = new List<CurveLoop>();
      foreach (var curveArray in speckleToposolid.profiles)
      {
        curveLoops.Add(CurveArrayToCurveLoop(CurveToNative(curveArray.ToList())));
      }
      
      // var curves = speckleToposolid.profiles.Select(x => CurveToNative(x));
      // var curveLoops = curves.Select(x => CurveArrayToCurveLoop(x)).ToList();
      
      var points = speckleToposolid.points.Select(x => PointToNative(x)).ToList();
      
      bool structural = false;
      if (speckleToposolid["structural"] is bool isStructural)
        structural = isStructural;

      DB.Level level;
      double slope = 0;
      DB.Line slopeDirection = null;
      level = ConvertLevelToRevit(speckleToposolid.level, out ApplicationObject.State state);
      structural = speckleToposolid.structural;
      slope = speckleToposolid.slope;
      slopeDirection = (speckleToposolid.slopeDirection != null) ? LineToNative(speckleToposolid.slopeDirection) : null;

      var topoSolidType = GetElementType<ToposolidType>(speckleToposolid, appObj, out bool _);

      Toposolid revitToposolid = null;
      try
      {
        revitToposolid = Toposolid.Create(Doc, curveLoops, points, topoSolidType.Id, level.Id);
        SetInstanceParameters(revitToposolid, speckleToposolid);
      }
      catch (Exception ex)
      {
        Debug.WriteLine(ex.Message);
      }

      // appObj.Update(status: ApplicationObject.State.Created, createdId: revitFloor.UniqueId, convertedItem: revitFloor);
      appObj.Update(status: ApplicationObject.State.Created, createdId: revitToposolid.UniqueId, convertedItem: revitToposolid);
      return appObj;
    }
    
    // Nesting the various profiles into a polycurve segments.
    private List<ICurve> GetAllCurves(Toposolid toposolid)
    {
      var profiles = new List<ICurve>();
      var faces = HostObjectUtils.GetTopFaces(toposolid);

      foreach (var hostObject in faces)
      {
        var face = toposolid.GetGeometryObjectFromReference(hostObject) as Face;
        var crvLoops = face.GetEdgesAsCurveLoops();
        foreach (var crvloop in crvLoops)
        {
          var poly = new Polycurve(ModelUnits);
          foreach (var curve in crvloop)
          {
            var c = curve;
            if (c == null)
            {
              // TODO: should be logging "WARNING: unsupported curve"
              continue;
            }

            poly.segments.Add(CurveToSpeckle(c, toposolid.Document));
          }

          profiles.Add(poly);
        }
      }

      return profiles;
    }

    private List<ICurve[]> GetAllCurves(Sketch sketch)
    {
      var profiles = new List<ICurve[]>();
      foreach (CurveArray curves in sketch.Profile)
      {
        var curveLoop = CurveArrayToCurveLoop(curves);
        profiles.Add(curveLoop.Select(x => CurveToSpeckle(x, sketch.Document)).ToArray());
      }

      return profiles;
    }
    
    private SpeckleToposolid ToposolidToSpeckle(Toposolid topoSolid, out List<string> notes)
    { 
      var speckleToposolid = new SpeckleToposolid();
      notes = new List<string>();

      // we will store the list of interior points in order to recreate the Toposolid
      var slabShapeEditor = topoSolid.GetSlabShapeEditor();
      var vertices = slabShapeEditor.SlabShapeVertices;
      speckleToposolid.points = vertices.Cast<SlabShapeVertex>()
        // possibly not needed
        //    .Where(x => x.VertexType == SlabShapeVertexType.Interior)
        .Select(x => new OG.Point(x.Position.X, x.Position.Y, x.Position.Z))
        .ToList();

      var sketch = Doc.GetElement(topoSolid.SketchId) as Sketch;
      speckleToposolid.profiles = GetAllCurves(sketch);

      var type = topoSolid.Document.GetElement(topoSolid.GetTypeId()) as ElementType;

      //
      speckleToposolid.level = ConvertAndCacheLevel(topoSolid, BuiltInParameter.LEVEL_PARAM);
      
      speckleToposolid.structural = GetParamValue<bool>(topoSolid, BuiltInParameter.FLOOR_PARAM_IS_STRUCTURAL);
  
      var slopeParam = GetParamValue<double?>(topoSolid, BuiltInParameter.ROOF_SLOPE) / 100;
      
      GetAllRevitParamsAndIds(
        speckleToposolid,
        topoSolid,
        new List<string>
        {
          "LEVEL_PARAM",
          "FLOOR_PARAM_IS_STRUCTURAL",
          "ROOF_SLOPE"
        }
      );
      
      var slopeArrow = GetSlopeArrow(topoSolid);
      if (slopeArrow != null)
      {
        var tail = GetSlopeArrowTail(slopeArrow, Doc);
        var head = GetSlopeArrowHead(slopeArrow, Doc);
        var tailOffset = GetSlopeArrowTailOffset(slopeArrow, Doc);
        _ = GetSlopeArrowHeadOffset(slopeArrow, Doc, tailOffset, out var slope);
      
        slopeParam ??= slope;
        speckleToposolid.slope = (double) slopeParam;
        
        speckleToposolid.slopeDirection = new Geometry.Line(tail, head);
        if (speckleToposolid["parameters"] is Base parameters &&
            parameters["FLOOR_HEIGHTABOVELEVEL_PARAM"] is BuiltElements.Revit.Parameter offsetParam &&
            offsetParam.value is double offset)
        {
          offsetParam.value = offset + tailOffset;
        }
      }
      
      speckleToposolid.displayValue = GetElementDisplayValue(
        topoSolid,
        new Options() { DetailLevel = ViewDetailLevel.Fine });
      
      GetHostedElements(speckleToposolid, topoSolid, out List<string> hostedNotes);
      if (hostedNotes.Any())
        notes.AddRange(hostedNotes);

      return speckleToposolid;
    }
  }
}
