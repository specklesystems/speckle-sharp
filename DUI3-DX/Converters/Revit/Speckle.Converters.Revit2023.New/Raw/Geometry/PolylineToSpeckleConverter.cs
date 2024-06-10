using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Revit.Interfaces;

namespace Speckle.Converters.Revit2023.ToSpeckle;

public class PolylineToSpeckleConverter : ITypedConverter<IRevitPolyLine, SOG.Polyline>
{
  private readonly IConversionContextStack<IRevitDocument, IRevitForgeTypeId> _contextStack;
  private readonly ITypedConverter<IRevitXYZ, SOG.Point> _xyzToPointConverter;

  public PolylineToSpeckleConverter( IConversionContextStack<IRevitDocument, IRevitForgeTypeId> contextStack,
    ITypedConverter<IRevitXYZ, SOG.Point> xyzToPointConverter
  )
  {
    _contextStack = contextStack;
    _xyzToPointConverter = xyzToPointConverter;
  }

  public SOG.Polyline Convert(IRevitPolyLine target)
  {
    var coords = target.GetCoordinates().SelectMany(coord => _xyzToPointConverter.Convert(coord).ToList()).ToList();
    return new SOG.Polyline(coords, _contextStack.Current.SpeckleUnits);
  }
}
