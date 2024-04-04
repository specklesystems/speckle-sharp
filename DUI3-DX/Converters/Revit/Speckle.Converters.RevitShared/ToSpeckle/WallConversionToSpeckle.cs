using Speckle.Converters.Common.Objects;
using Speckle.Converters.Common;
using Objects.BuiltElements.Revit;
using Autodesk.Revit.DB;
using Objects;
using Speckle.Converters.RevitShared.Helpers;
using System.Collections.Generic;
using System.Linq;
using Objects.Geometry;
using Speckle.Core.Models;
using Speckle.Core.Models.Extensions;

namespace Speckle.Converters.RevitShared.ToSpeckle;

// POC: needs review feels, BIG, feels like it could be broken down..
// i.e. GetParams(), GetGeom()? feels like it's doing too much
[NameAndRankValue(nameof(DB.Wall), 0)]
public class WallConversionToSpeckle : BaseConversionToSpeckle<DB.Wall, RevitWall>
{
  private readonly IRawConversion<DB.Curve, ICurve> _curveConverter;
  private readonly IRawConversion<DB.Level, RevitLevel> _levelConverter;
  private readonly IRawConversion<DB.CurveArray, SOG.Polycurve> _curveArrayConverter;
  private readonly ParameterValueExtractor _parameterValueExtractor;
  private readonly RevitConversionContextStack _contextStack;
  private readonly DisplayValueExtractor _displayValueExtractor;
  private readonly HostedElementConversionToSpeckle _hostedElementConverter;
  private readonly ParameterObjectAssigner _parameterObjectAssigner;

  public WallConversionToSpeckle(
    IRawConversion<DB.Curve, ICurve> curveConverter,
    IRawConversion<Level, RevitLevel> levelConverter,
    RevitConversionContextStack contextStack,
    ParameterValueExtractor parameterValueExtractor,
    DisplayValueExtractor displayValueExtractor,
    IRawConversion<CurveArray, Polycurve> curveArrayConverter,
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

  public override RevitWall RawConvert(DB.Wall target)
  {
    RevitWall speckleWall = new() { family = target.WallType.FamilyName.ToString(), type = target.WallType.Name };

    if (target.Location is not LocationCurve locationCurve)
    {
      throw new SpeckleConversionException(
        "Incorrect assumption was made that all Revit Wall location properties would be of type \"LocationCurve\""
      );
    }

    speckleWall.baseLine = _curveConverter.RawConvert(locationCurve.Curve);

    var level = _parameterValueExtractor.GetValueAsDocumentObject<DB.Level>(
      target,
      BuiltInParameter.WALL_BASE_CONSTRAINT
    );
    speckleWall.level = _levelConverter.RawConvert(level);

    var topLevel = _parameterValueExtractor.GetValueAsDocumentObject<DB.Level>(
      target,
      BuiltInParameter.WALL_BASE_CONSTRAINT
    );
    speckleWall.topLevel = _levelConverter.RawConvert(topLevel);

    // POC : what to do if these parameters are unset (instead of assigning default)
    speckleWall.height =
      _parameterValueExtractor.GetValueAsDouble(target, BuiltInParameter.WALL_USER_HEIGHT_PARAM) ?? 0;
    speckleWall.baseOffset = _parameterValueExtractor.GetValueAsDouble(target, BuiltInParameter.WALL_BASE_OFFSET) ?? 0;
    speckleWall.topOffset = _parameterValueExtractor.GetValueAsDouble(target, BuiltInParameter.WALL_TOP_OFFSET) ?? 0;
    speckleWall.structural =
      _parameterValueExtractor.GetValueAsBool(target, BuiltInParameter.WALL_STRUCTURAL_SIGNIFICANT) ?? false;
    speckleWall.flipped = target.Flipped;

    speckleWall.displayValue = _displayValueExtractor.GetDisplayValue(target);

    AssignVoids(target, speckleWall);
    AssignHostedElements(target, speckleWall);

    _parameterObjectAssigner.AssignParametersToBase(target, speckleWall);

    return speckleWall;
  }

  // POC: not sure
  private void AssignHostedElements(Wall target, RevitWall speckleWall)
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

  private void AssignVoids(Wall target, RevitWall speckleWall)
  {
    List<CurveArray> voids = GetWallVoids(target);
    List<Polycurve> polycurves = voids.Select(v => _curveArrayConverter.RawConvert(v)).ToList();

    if (polycurves.Count > 0)
    {
      speckleWall["voids"] = polycurves;
    }
  }

  private List<CurveArray> GetWallVoids(Wall wall)
  {
    List<CurveArray> curveArrays = new();
    var profile = ((Sketch)_contextStack.Current.Document.Document.GetElement(wall.SketchId))?.Profile;

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
