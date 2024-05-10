using Objects;
using Objects.Geometry;
using Speckle.Core.Models;

namespace Speckle.Converters.Common;

// POC: This object was created with the purpose of defining a specific subtype of base type the fallback conversion.
// This will never be serialised and is just used to provide a type to register as the fallback conversion.
// We did this because providing a conversion from List<Base> seemed too generic and potentially conflicting.
// This also allows us to treat any implementation of IDisplayValue<T> without caring about it's specific T type.

public sealed class DisplayableObject : Base, IDisplayValue<IReadOnlyList<Base>>
{
  public DisplayableObject(IReadOnlyList<Base> displayValue)
  {
    var invalidGeometries = displayValue
      .Where(b => b is not (Line or Polyline or Mesh))
      .Select(b => b.GetType())
      .Distinct();

    if (invalidGeometries.Any())
    {
      throw new ArgumentException(
        $"Displayable objects should only contain simple geometries (lines, polylines, meshes) but contained {invalidGeometries}"
      );
    }

    this.displayValue = displayValue;
  }

  public IReadOnlyList<Base> displayValue { get; }
}
