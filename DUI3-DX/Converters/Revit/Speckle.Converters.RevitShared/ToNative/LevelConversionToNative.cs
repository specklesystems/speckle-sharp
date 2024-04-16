using Autodesk.Revit.DB;
using Objects.BuiltElements.Revit;
using Speckle.Converters.Common;
using Speckle.Converters.RevitShared.Helpers;
using Speckle.Converters.RevitShared.Services;

namespace Speckle.Converters.RevitShared.ToNative;

[NameAndRankValue(nameof(DB.Level), 0)]
public class LevelConversionToNative : BaseConversionToHost<SOB.Level, DB.Level>
{
  private readonly RevitConversionContextStack _contextStack;

  public LevelConversionToNative(RevitConversionContextStack contextStack)
  {
    _contextStack = contextStack;
  }

  public override DB.Level RawConvert(SOB.Level target)
  {
    using var documentLevelCollector = new FilteredElementCollector(_contextStack.Current.Document.Document);
    var docLevels = documentLevelCollector.OfClass(typeof(DB.Level)).ToElements().Cast<DB.Level>();

    // POC : I'm not really understanding the linked use case for this. Do we want to bring this over?

    //bool elevationMatch = true;
    ////level by name component
    //if (target is RevitLevel speckleRevitLevel && speckleRevitLevel.referenceOnly)
    //{
    //  //see: https://speckle.community/t/revit-connector-levels-and-spaces/2824/5
    //  elevationMatch = false;
    //  if (GetExistingLevelByName(docLevels, target.name) is DB.Level existingLevelWithSameName)
    //  {
    //    return existingLevelWithSameName;
    //  }
    //}

    DB.Level revitLevel;
    var targetElevation = ToHostScalingService.Scale(target.elevation, target.units);

    if (GetExistingLevelByElevation(docLevels, targetElevation) is Level existingLevel)
    {
      revitLevel = existingLevel;
    }
    else
    {
      revitLevel = Level.Create(_contextStack.Current.Document.Document, targetElevation);
      revitLevel.Name = target.name;

      if (target is RevitLevel rl && rl.createView)
      {
        using var viewPlan = CreateViewPlan(target.name, revitLevel.Id);
      }
    }

    return revitLevel;
  }

  private static Level GetExistingLevelByElevation(IEnumerable<Level> docLevels, double elevation)
  {
    return docLevels.FirstOrDefault(l => Math.Abs(l.Elevation - elevation) < RevitConversionContextStack.TOLERANCE);
  }

  private ViewPlan CreateViewPlan(string name, ElementId levelId)
  {
    using var collector = new FilteredElementCollector(_contextStack.Current.Document.Document);
    var vt = collector
      .OfClass(typeof(ViewFamilyType))
      .First(el => ((ViewFamilyType)el).ViewFamily == ViewFamily.FloorPlan);

    var view = ViewPlan.Create(_contextStack.Current.Document.Document, vt.Id, levelId);
    try
    {
      view.Name = name;
    }
    catch (Autodesk.Revit.Exceptions.ApplicationException)
    {
      // POC : logging
    }

    return view;
  }
}
