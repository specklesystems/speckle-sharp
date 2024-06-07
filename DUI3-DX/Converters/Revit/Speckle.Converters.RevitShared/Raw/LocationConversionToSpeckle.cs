using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;
using Speckle.Revit.Api;
using Speckle.Revit.Interfaces;

namespace Speckle.Converters.RevitShared.ToSpeckle;

public class LocationConversionToSpeckle : ConverterAdapter<DB.Location, IRevitLocation, LocationProxy, Base>
{
  public LocationConversionToSpeckle(ITypedConverter<IRevitLocation, Base> converter) : base(converter)
  {
  }

  protected override LocationProxy Create(DB.Location target) => new (target);
}
