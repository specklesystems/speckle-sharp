using Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.Rhino7.ToHost;

// POC: This object was created with the purpose of defining a specific subtype of base type the fallback conversion.
// This will never be serialised and is just used to provide a type to register as the fallback conversion.
// We did this because providing a conversion from List<Base> seemed too generic and potentially conflicting.
// This also allows us to treat any implementation of IDisplayValue<T> without caring about it's specific T type.

public class DisplayableObject : Base, IDisplayValue<List<Base>>
{
  public DisplayableObject(IReadOnlyList<Base> displayValue)
  {
    var hasInvalidGeometries = displayValue.Any(b => b is not (SOG.Line or SOG.Polyline or SOG.Mesh));

    if (hasInvalidGeometries)
    {
      throw new ArgumentException(
        "Displayable objects should only contain simple geometries (lines, polylines, meshes)"
      );
    }

    this.displayValue = displayValue.ToList();
  }

  public List<Base> displayValue { get; set; }
}
