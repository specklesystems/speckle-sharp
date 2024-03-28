using System.Diagnostics.CodeAnalysis;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using Speckle.Converters.Common;

namespace Speckle.Converters.ArcGIS3;

// POC: Suppressed naming warning for now, but we should evaluate if we should follow this or disable it.
[SuppressMessage(
  "Naming",
  "CA1711:Identifiers should not have incorrect suffix",
  Justification = "Name ends in Stack but it is in fact a Stack, just not inheriting from `System.Collections.Stack`"
)]
public class ArcGISConversionContextStack : ConversionContextStack<Map, Unit>
{
  public ArcGISConversionContextStack(IHostToSpeckleUnitConverter<Unit> unitConverter)
    : base(MapView.Active.Map, MapView.Active.Map.SpatialReference.Unit, unitConverter) { }
}
