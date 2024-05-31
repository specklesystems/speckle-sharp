using System.Diagnostics.CodeAnalysis;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Mapping;
using Speckle.Converters.Common;

namespace Speckle.Converters.ArcGIS3;

public record ArcGISDocument(Project Project, Map Map);

// POC: Suppressed naming warning for now, but we should evaluate if we should follow this or disable it.
[SuppressMessage(
  "Naming",
  "CA1711:Identifiers should not have incorrect suffix",
  Justification = "Name ends in Stack but it is in fact a Stack, just not inheriting from `System.Collections.Stack`"
)]
public class ArcGISConversionContextStack : ConversionContextStack<ArcGISDocument, ACG.Unit>
{
  public ArcGISConversionContextStack(IHostToSpeckleUnitConverter<ACG.Unit> unitConverter)
    : base(
      new ArcGISDocument(Project.Current, MapView.Active.Map),
      MapView.Active.Map.SpatialReference.Unit,
      unitConverter
    ) { }
}
