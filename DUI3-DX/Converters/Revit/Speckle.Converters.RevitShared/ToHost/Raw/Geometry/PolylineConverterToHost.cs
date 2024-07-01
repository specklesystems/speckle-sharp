using Autodesk.Revit.DB;
using Objects.Geometry;
using Speckle.Converters.Common.Objects;
using Speckle.Converters.RevitShared.Helpers;
using Speckle.Converters.RevitShared.Services;

namespace Speckle.Converters.RevitShared.ToSpeckle;

public class PolylineConverterToHost : ITypedConverter<SOG.Polyline, DB.CurveArray>
{
  private readonly ITypedConverter<SOG.Line, DB.Line> _lineConverter;
  private readonly ScalingServiceToHost _scalingService;
  private readonly IRevitConversionContextStack _contextStack;

  public PolylineConverterToHost(
    ITypedConverter<SOG.Line, DB.Line> lineConverter,
    ScalingServiceToHost scalingService,
    IRevitConversionContextStack contextStack
  )
  {
    _lineConverter = lineConverter;
    _scalingService = scalingService;
    _contextStack = contextStack;
  }

  public CurveArray Convert(Polyline target)
  {
    var curveArray = new CurveArray();
    if (target.value.Count == 6)
    {
      // 6 coordinate values (two sets of 3), so polyline is actually a single line
      curveArray.Append(_lineConverter.Convert(new SOG.Line(target.value, target.units)));
    }
    else
    {
      var pts = target.GetPoints();
      var lastPt = pts[0];
      for (var i = 1; i < pts.Count; i++)
      {
        var success = TryAppendLineSafely(curveArray, new SOG.Line(lastPt, pts[i], target.units));
        if (success)
        {
          lastPt = pts[i];
        }
      }

      if (target.closed)
      {
        TryAppendLineSafely(curveArray, new SOG.Line(pts[^1], pts[0], target.units));
      }
    }
    return curveArray;
  }

  /// <summary>
  /// Checks if a Speckle <see cref="SOG.Line"/> is too sort to be created in Revit.
  /// </summary>
  /// <remarks>
  /// The length of the line will be computed on the spot to ensure it is accurate.
  /// </remarks>
  /// <param name="line">The <see cref="SOG.Line"/> to be tested.</param>
  /// <returns>true if the line is too short, false otherwise.</returns>
  public bool IsLineTooShort(SOG.Line line)
  {
    var scaleToNative = _scalingService.ScaleToNative(SOG.Point.Distance(line.start, line.end), line.units);
    return scaleToNative < _contextStack.Current.Document.Application.ShortCurveTolerance;
  }

  /// <summary>
  /// Attempts to append a Speckle <see cref="SOG.Line"/> onto a Revit <see cref="CurveArray"/>.
  /// This method ensures the line is long enough to be supported.
  /// It will also convert the line to Revit before appending it to the <see cref="CurveArray"/>.
  /// </summary>
  /// <param name="curveArray">The revit <see cref="CurveArray"/> to add the line to.</param>
  /// <param name="line">The <see cref="SOG.Line"/> to be added.</param>
  /// <returns>True if the line was added, false otherwise.</returns>
  public bool TryAppendLineSafely(CurveArray curveArray, SOG.Line line)
  {
    if (IsLineTooShort(line))
    {
      // poc : logging "Some lines in the CurveArray where ignored due to being smaller than the allowed curve length."
      return false;
    }

    curveArray.Append(_lineConverter.Convert(line));
    return true;
  }
}
