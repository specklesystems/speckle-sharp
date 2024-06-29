using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.ArcGIS3.Geometry;

public class PointToHostConverter : ITypedConverter<SOG.Point, ACG.MapPoint>
{
  private readonly IConversionContextStack<ArcGISDocument, ACG.Unit> _contextStack;

  public PointToHostConverter(IConversionContextStack<ArcGISDocument, ACG.Unit> contextStack)
  {
    _contextStack = contextStack;
  }

  public object Convert(Base target) => Convert((SOG.Point)target);

  public ACG.MapPoint Convert(SOG.Point target)
  {
    SOG.Point scaledMovedRotatedPoint = _contextStack.Current.Document.ActiveCRSoffsetRotation.OffsetRotateOnReceive(
      target
    );
    return new ACG.MapPointBuilderEx(
      scaledMovedRotatedPoint.x,
      scaledMovedRotatedPoint.y,
      scaledMovedRotatedPoint.z
    ).ToGeometry();
  }
}
