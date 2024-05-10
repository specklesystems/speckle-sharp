namespace Speckle.Converters.Common;

public interface IHostToSpeckleUnitConverter<in THostUnit>
{
  string ConvertOrThrow(THostUnit hostUnit);
}
