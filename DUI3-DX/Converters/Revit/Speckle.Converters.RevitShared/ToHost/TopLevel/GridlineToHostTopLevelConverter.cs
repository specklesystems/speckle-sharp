using Objects;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Converters.RevitShared.Helpers;
using Speckle.Converters.RevitShared.ToSpeckle;

namespace Speckle.Converters.RevitShared.ToHost.TopLevel;

[NameAndRankValue(nameof(SOBE.GridLine), 0)]
internal class GridlineToHostTopLevelConverter : BaseTopLevelConverterToHost<SOBE.GridLine, DB.Grid>
{
  private readonly ITypedConverter<ICurve, DB.CurveArray> _curveConverter;
  private readonly IRevitConversionContextStack _contextStack;

  public GridlineToHostTopLevelConverter(
    ITypedConverter<ICurve, DB.CurveArray> curveConverter,
    IRevitConversionContextStack contextStack
  )
  {
    _curveConverter = curveConverter;
    _contextStack = contextStack;
  }

  public override DB.Grid Convert(SOBE.GridLine target)
  {
    DB.Curve curve = _curveConverter.Convert(target.baseLine).get_Item(0);

    using DB.Grid revitGrid = curve switch
    {
      DB.Arc arc => DB.Grid.Create(_contextStack.Current.Document, arc),
      DB.Line line => DB.Grid.Create(_contextStack.Current.Document, line),
      _ => throw new SpeckleConversionException($"Grid line curve is of type {curve.GetType()} which is not supported")
    };

    if (!string.IsNullOrEmpty(target.label) && !GridNameIsTaken(target.label))
    {
      revitGrid.Name = target.label;
    }

    return revitGrid;
  }

  private bool GridNameIsTaken(string gridName)
  {
    using var collector = new DB.FilteredElementCollector(_contextStack.Current.Document);

    IEnumerable<string> gridNames = collector
      .WhereElementIsNotElementType()
      .OfClass(typeof(DB.Grid))
      .ToElements()
      .Cast<DB.Grid>()
      .Select(grid => grid.Name);

    return gridNames.Contains(gridName);
  }
}
