using Objects.Primitive;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.Geometry
{
  /// <summary>
  /// Represents a 3-dimensional box oriented on a plane.
  /// </summary>
  public class Box : Base, IHasVolume, IHasArea, IHasBoundingBox
  {
    /// <summary>
    /// Gets or sets the plane that defines the orientation of the <see cref="Box"/>
    /// </summary>
    public Plane basePlane { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="Interval"/> that defines the min and max coordinate in the X direction
    /// </summary>
    public Interval xSize { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="Interval"/> that defines the min and max coordinate in the Y direction
    /// </summary>
    public Interval ySize { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="Interval"/> that defines the min and max coordinate in the Y direction
    /// </summary>
    public Interval zSize { get; set; }

    /// <inheritdoc/>
    public Box bbox { get; }

    /// <inheritdoc/>
    public double area { get; set; }

    /// <inheritdoc/>
    public double volume { get; set; }

    /// <summary>
    /// The units this object's coordinates are in.
    /// </summary>
    /// <remarks>
    /// This should be one of <see cref="Speckle.Core.Kits.Units"/>
    /// </remarks>
    public string units { get; set; }

    /// <inheritdoc/>
    public Box() { }

    /// <summary>
    /// Constructs a new <see cref="Box"/> instance with a <see cref="Plane"/> and coordinate intervals for all 3 axis {x , y , z}
    /// </summary>
    /// <param name="basePlane">The plane the box will be oriented by.</param>
    /// <param name="xSize">The range of coordinates (min, max) for the X axis</param>
    /// <param name="ySize">The range of coordinates (min, max) for the Y axis</param>
    /// <param name="zSize">The range of coordinates (min, max) for the Z axis</param>
    /// <param name="units">The units the coordinates are in.</param>
    /// <param name="applicationId">The unique application ID of the object.</param>
    public Box(Plane basePlane, Interval xSize, Interval ySize, Interval zSize, string units = Units.Meters, string applicationId = null)
    {
      this.basePlane = basePlane;
      this.xSize = xSize;
      this.ySize = ySize;
      this.zSize = zSize;
      this.applicationId = applicationId;
      this.units = units;
    }
  }
}
