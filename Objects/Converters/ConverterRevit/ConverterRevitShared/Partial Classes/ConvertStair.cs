
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Objects.BuiltElements.Revit;
using System.Collections.Generic;
using System.Linq;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    //NOTE: we are currently only converting Stairs ToSpeckle
    //a ToNative method might come later on!
    private RevitStair StairToSpeckle(Stairs revitStair)
    {
      var stairType = revitStair.Document.GetElement(revitStair.GetTypeId()) as StairsType;
      var speckleStair = new RevitStair();
      speckleStair.family = stairType.FamilyName;
      speckleStair.type = stairType.Name;
      speckleStair.level = ConvertAndCacheLevel(revitStair, BuiltInParameter.STAIRS_BASE_LEVEL_PARAM);
      speckleStair.topLevel = ConvertAndCacheLevel(revitStair, BuiltInParameter.STAIRS_TOP_LEVEL_PARAM);
      speckleStair.riserHeight = ScaleToSpeckle(revitStair.ActualRiserHeight);
      speckleStair.risersNumber = revitStair.ActualRisersNumber;
      speckleStair.treadDepth = ScaleToSpeckle(revitStair.ActualTreadDepth);
      speckleStair.treadsNumber = revitStair.ActualTreadsNumber;
      speckleStair.baseElevation = ScaleToSpeckle(revitStair.BaseElevation);
      speckleStair.topElevation = ScaleToSpeckle(revitStair.TopElevation);
      speckleStair.height = ScaleToSpeckle(revitStair.Height);
      speckleStair.numberOfStories = revitStair.NumberOfStories;

      speckleStair.runs = revitStair.GetStairsRuns().Select(x => StairRunToSpeckle(revitStair.Document.GetElement(x) as StairsRun)).ToList();
      speckleStair.landings = revitStair.GetStairsLandings().Select(x => StairLandingToSpeckle(revitStair.Document.GetElement(x) as StairsLanding)).ToList();
      speckleStair.supports = revitStair.GetStairsSupports().Select(x => StairSupportToSpeckle(revitStair.Document.GetElement(x))).ToList();

      GetAllRevitParamsAndIds(speckleStair, revitStair, new List<string> { "STAIRS_BASE_LEVEL_PARAM", "STAIRS_TOP_LEVEL_PARAM" });

      speckleStair.displayValue = GetElementDisplayMesh(revitStair, new Options() { DetailLevel = ViewDetailLevel.Fine, ComputeReferences = false });

      return speckleStair;
    }

    private RevitStairRun StairRunToSpeckle(StairsRun revitStairRun)
    {
      var stairType = revitStairRun.Document.GetElement(revitStairRun.GetTypeId()) as StairsRunType;
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

      GetAllRevitParamsAndIds(run, revitStairRun);
      return run;
    }

    private RevitStairLanding StairLandingToSpeckle(StairsLanding revitStairLanding)
    {
      var stairType = revitStairLanding.Document.GetElement(revitStairLanding.GetTypeId()) as StairsLandingType;
      var landing = new RevitStairLanding();
      landing.family = stairType.FamilyName;
      landing.type = stairType.Name;
      landing.isAutomaticLanding = revitStairLanding.IsAutomaticLanding;
      landing.thickness = revitStairLanding.Thickness;
      landing.baseElevation = ScaleToSpeckle(revitStairLanding.BaseElevation);
      landing.units = ModelUnits;
      landing.outline = CurveLoopToSpeckle(revitStairLanding.GetFootprintBoundary());

      GetAllRevitParamsAndIds(landing, revitStairLanding);
      return landing;
    }

    private RevitStairSupport StairSupportToSpeckle(Element revitStairSupport)
    {
      var stairType = revitStairSupport.Document.GetElement(revitStairSupport.GetTypeId()) as ElementType;
      var support = new RevitStairSupport();
      support.family = stairType.FamilyName;
      support.type = stairType.Name;

      GetAllRevitParamsAndIds(support, revitStairSupport);
      return support;
    }
  }
}
