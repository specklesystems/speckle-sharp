using Speckle.Converters.Common.Objects;
using Speckle.Core.Logging;
using Speckle.Rhino7.Interfaces;

namespace Speckle.Converters.Rhino7.ToHost.Raw;

/// <summary>
/// Converts a flat list of raw double values to a Point3dList.
/// </summary>
public class FlatPointListToHostConverter : ITypedConverter<IReadOnlyList<double>, IRhinoPoint3dList>
{
  private readonly IRhinoPointFactory _rhinoPointFactory;

  public FlatPointListToHostConverter(IRhinoPointFactory rhinoPointFactory)
  {
    _rhinoPointFactory = rhinoPointFactory;
  }

  /// <summary>
  /// Converts a flat list of raw double values to a Point3dList.
  /// </summary>
  /// <param name="target">The flat list of raw double values</param>
  /// <returns>A Point3dList object that represents the converted points</returns>
  /// <remarks>
  /// Assumes that the amount of numbers contained on the list is a multiple of 3,
  /// with the numbers being coordinates of each point in the format {x1, y1, z1, x2, y2, z2, ..., xN, yN, zN}
  /// </remarks>
  /// <exception cref="SpeckleException">Throws when the input list count is not a multiple of 3.</exception>
  public IRhinoPoint3dList Convert(IReadOnlyList<double> target)
  {
    if (target.Count % 3 != 0)
    {
      throw new SpeckleException("Array malformed: length%3 != 0.");
    }

    var points = new List<IRhinoPoint3d>(target.Count / 3);

    for (int i = 2; i < target.Count; i += 3)
    {
      points.Add(_rhinoPointFactory.Create(target[i - 2], target[i - 1], target[i]));
    }

    return _rhinoPointFactory.Create(points);
  }
}
