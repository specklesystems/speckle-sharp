using Speckle.Converters.Common.Objects;
using Speckle.Converters.Common;
using Objects;
using Speckle.Converters.RevitShared.Helpers;
using Speckle.Core.Models;
using Speckle.Core.Models.Extensions;

namespace Speckle.Converters.RevitShared.ToSpeckle;

// POC: needs review feels, BIG, feels like it could be broken down..
// i.e. GetParams(), GetGeom()? feels like it's doing too much
[NameAndRankValue(nameof(DB.Wall), 0)]
public class WallConversionToSpeckle : BaseConversionToSpeckle<DB.Wall, SOBR.RevitWall>
{
  private readonly IRawConversion<DB.Curve, ICurve> _curveConverter;
  private readonly IRawConversion<DB.Level, SOBR.RevitLevel> _levelConverter;
  private readonly IRawConversion<DB.CurveArray, SOG.Polycurve> _curveArrayConverter;
  private readonly ParameterValueExtractor _parameterValueExtractor;
  private readonly RevitConversionContextStack _contextStack;
  private readonly DisplayValueExtractor _displayValueExtractor;
  private readonly HostedElementConversionToSpeckle _hostedElementConverter;
  private readonly ParameterObjectAssigner _parameterObjectAssigner;

  public WallConversionToSpeckle(
    IRawConversion<DB.Curve, ICurve> curveConverter,
    IRawConversion<DB.Level, SOBR.RevitLevel> levelConverter,
    RevitConversionContextStack contextStack,
    ParameterValueExtractor parameterValueExtractor,
    DisplayValueExtractor displayValueExtractor,
    IRawConversion<DB.CurveArray, SOG.Polycurve> curveArrayConverter,
    HostedElementConversionToSpeckle hostedElementConverter,
    ParameterObjectAssigner parameterObjectAssigner
  )
  {
    _curveConverter = curveConverter;
    _levelConverter = levelConverter;
    _contextStack = contextStack;
    _parameterValueExtractor = parameterValueExtractor;
    _displayValueExtractor = displayValueExtractor;
    _curveArrayConverter = curveArrayConverter;
    _hostedElementConverter = hostedElementConverter;
    _parameterObjectAssigner = parameterObjectAssigner;
  }

  public override SOBR.RevitWall RawConvert(DB.Wall target)
  {
    SOBR.RevitWall speckleWall = new() { family = target.WallType.FamilyName.ToString(), type = target.WallType.Name };

    if (target.Location is not DB.LocationCurve locationCurve)
    {
      throw new SpeckleConversionException(
        "Incorrect assumption was made that all Revit Wall location properties would be of type \"LocationCurve\""
      );
    }

    speckleWall.baseLine = _curveConverter.RawConvert(locationCurve.Curve);

    var level = _parameterValueExtractor.GetValueAsDocumentObject<DB.Level>(
      target,
      DB.BuiltInParameter.WALL_BASE_CONSTRAINT
    );
    speckleWall.level = _levelConverter.RawConvert(level);

    var topLevel = _parameterValueExtractor.GetValueAsDocumentObject<DB.Level>(
      target,
      DB.BuiltInParameter.WALL_BASE_CONSTRAINT
    );
    speckleWall.topLevel = _levelConverter.RawConvert(topLevel);

    // POC : what to do if these parameters are unset (instead of assigning default)
    speckleWall.height =
      _parameterValueExtractor.GetValueAsDouble(target, DB.BuiltInParameter.WALL_USER_HEIGHT_PARAM) ?? 0;
    speckleWall.baseOffset =
      _parameterValueExtractor.GetValueAsDouble(target, DB.BuiltInParameter.WALL_BASE_OFFSET) ?? 0;
    speckleWall.topOffset = _parameterValueExtractor.GetValueAsDouble(target, DB.BuiltInParameter.WALL_TOP_OFFSET) ?? 0;
    speckleWall.structural =
      _parameterValueExtractor.GetValueAsBool(target, DB.BuiltInParameter.WALL_STRUCTURAL_SIGNIFICANT) ?? false;
    speckleWall.flipped = target.Flipped;

    // POC: shouldn't we just do this in the RevitConverterToSpeckle ?
    speckleWall.displayValue = _displayValueExtractor.GetDisplayValue(target);

    AssignVoids(target, speckleWall);
    AssignHostedElements(target, speckleWall);

    // POC: shouldn't we just do this in the RevitConverterToSpeckle ?
    _parameterObjectAssigner.AssignParametersToBase(target, speckleWall);

    return speckleWall;
  }

  // POC: not sure
  private void AssignHostedElements(DB.Wall target, SOBR.RevitWall speckleWall)
  {
    List<Base> hostedObjects = _hostedElementConverter.GetHostedElementsConverted(target);
    if (hostedObjects.Count > 0)
    {
      if (speckleWall.GetDetachedProp("elements") is List<Base> elements)
      {
        elements.AddRange(hostedObjects);
      }
      else
      {
        speckleWall.SetDetachedProp("elements", hostedObjects);
      }
    }
  }

  private void AssignVoids(DB.Wall target, SOBR.RevitWall speckleWall)
  {
    List<DB.CurveArray> voids = GetWallVoids(target);
    List<SOG.Polycurve> polycurves = voids.Select(v => _curveArrayConverter.RawConvert(v)).ToList();

    if (polycurves.Count > 0)
    {
      speckleWall["voids"] = polycurves;
    }
  }

  private List<DB.CurveArray> GetWallVoids(DB.Wall wall)
  {
    List<DB.CurveArray> curveArrays = new();
    var profile = ((DB.Sketch)_contextStack.Current.Document.Document.GetElement(wall.SketchId))?.Profile;

    if (profile == null)
    {
      return curveArrays;
    }

    for (var i = 1; i < profile.Size; i++)
    {
      var segments = profile.get_Item(i);
      if (segments.Cast<DB.Curve>().Count() > 2)
      {
        curveArrays.Add(segments);
      }
    }

    return curveArrays;
  }
}
