
using Autodesk.Revit.DB;
using Objects.BuiltElements.Revit;
using Objects.Geometry;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB.Architecture;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    private RevitStair StairToSpeckle(Stairs revitStair)
    {
      var baseLevelParam = revitStair.get_Parameter(BuiltInParameter.STAIRS_BASE_LEVEL_PARAM);
      var topLevelParam = revitStair.get_Parameter(BuiltInParameter.STAIRS_TOP_LEVEL_PARAM);

      var stairType = Doc.GetElement(revitStair.GetTypeId()) as StairsType;
      var speckleStair = new RevitStair();
      speckleStair.family = stairType.FamilyName;
      speckleStair.type = stairType.Name;
      speckleStair.level = ConvertAndCacheLevel(baseLevelParam);
      speckleStair.topLevel = ConvertAndCacheLevel(topLevelParam);
      speckleStair.riserHeight = ScaleToSpeckle(revitStair.ActualRiserHeight);
      speckleStair.risersNumber = revitStair.ActualRisersNumber;
      speckleStair.treradDepth = ScaleToSpeckle(revitStair.ActualTreadDepth);
      speckleStair.treadsNumber = revitStair.ActualTreadsNumber;
      speckleStair.baseElevation = ScaleToSpeckle(revitStair.BaseElevation);
      speckleStair.topElevation = ScaleToSpeckle(revitStair.TopElevation);
      speckleStair.height = ScaleToSpeckle(revitStair.Height);
      speckleStair.numberOfStories = revitStair.NumberOfStories;

      speckleStair.runs = revitStair.GetStairsRuns().Select(x => StairRunToSpeckle(Doc.GetElement(x) as StairsRun)).ToList();
      speckleStair.landings = revitStair.GetStairsLandings().Select(x => StairLandingToSpeckle(Doc.GetElement(x) as StairsLanding)).ToList();
      speckleStair.supports = revitStair.GetStairsSupports().Select(x => StairSupportToSpeckle(Doc.GetElement(x))).ToList();

      GetRevitParameters(speckleStair, revitStair, new List<string> { "STAIRS_BASE_LEVEL_PARAM", "STAIRS_TOP_LEVEL_PARAM" });

      var mesh = new Geometry.Mesh();
      (mesh.faces, mesh.vertices) = GetFaceVertexArrayFromElement(revitStair, new Options() { DetailLevel = ViewDetailLevel.Fine, ComputeReferences = false });

      speckleStair["@displayMesh"] = mesh;

      return speckleStair;
    }

    private RevitStairRun StairRunToSpeckle(StairsRun revitStairRun)
    {
      var stairType = Doc.GetElement(revitStairRun.GetTypeId()) as StairsRunType;
      var run = new RevitStairRun();
      run.family = stairType.FamilyName;
      run.type = stairType.Name;
      run.risersNumber = revitStairRun.ActualRisersNumber;
      run.runWidth = ScaleToSpeckle(revitStairRun.ActualRunWidth);
      run.treadsNumber = revitStairRun.ActualTreadsNumber;
      run.height = ScaleToSpeckle(revitStairRun.Height);
      run.baseElevation = ScaleToSpeckle(revitStairRun.BaseElevation);
      run.topElevation = ScaleToSpeckle(revitStairRun.TopElevation);
      run.beginsWithRiser = revitStairRun.BeginsWithRiser;
      run.endsWithRiser = revitStairRun.EndsWithRiser;
      run.extensionBelowRiserBase = ScaleToSpeckle(revitStairRun.ExtensionBelowRiserBase);
      run.extensionBelowTreadBase = ScaleToSpeckle(revitStairRun.ExtensionBelowTreadBase);
      run.runStyle = revitStairRun.StairsRunStyle.ToString();
      run.units = ModelUnits;
      run.path = CurveLoopToSpeckle(revitStairRun.GetStairsPath());
      run.outline = CurveLoopToSpeckle(revitStairRun.GetFootprintBoundary());

      GetRevitParameters(run, revitStairRun);
      return run;
    }

    private RevitStairLanding StairLandingToSpeckle(StairsLanding revitStairLanding)
    {
      var stairType = Doc.GetElement(revitStairLanding.GetTypeId()) as StairsLandingType;
      var landing = new RevitStairLanding();
      landing.family = stairType.FamilyName;
      landing.type = stairType.Name;
      landing.isAutomaticLanding = revitStairLanding.IsAutomaticLanding;
      landing.thickness = revitStairLanding.Thickness;
      landing.baseElevation = ScaleToSpeckle(revitStairLanding.BaseElevation);
      landing.units = ModelUnits;
      landing.outline = CurveLoopToSpeckle(revitStairLanding.GetFootprintBoundary());

      GetRevitParameters(landing, revitStairLanding);
      return landing;
    }

    private RevitStairSupport StairSupportToSpeckle(Element revitStairSupport)
    {
      var stairType = Doc.GetElement(revitStairSupport.GetTypeId()) as ElementType;
      var support = new RevitStairSupport();
      support.family = stairType.FamilyName;
      support.type = stairType.Name;

      GetRevitParameters(support, revitStairSupport);
      return support;
    }
  }
}
