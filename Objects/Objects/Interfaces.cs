using Objects.Geometry;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Objects
{
  /// <summary>
  /// Used to define a geometrical Base
  /// </summary>
  public interface IGeometry
  {
    /// <summary>
    /// Gets or sets the linear units assigned to this <see cref="IGeometry"/> instance.
    /// </summary>
    string linearUnits { get; set; }
  }

  public interface I3DGeometry : IGeometry
  {
    /// <summary>
    /// Gets or sets the bounding box for this <see cref="I3DGeometry"/> instance.
    /// </summary>
    Box boundingBox { get; set; }

    /// <summary>
    /// Gets or sets the center point for this <see cref="I3DGeometry"/> instance.
    /// </summary>
    Point center { get; set; }

    /// <summary>
    /// Gets or sets the volume for this <see cref="I3DGeometry"/> instance.
    /// </summary>
    double volume { get; set; }

    /// <summary>
    /// Gets or sets the area for this <see cref="I3DGeometry"/> instance.
    /// </summary>
    double area { get; set; }
  }

  public interface I2DGeometry : IGeometry
  {
    /// <summary>
    /// Gets or sets the bounding box for this <see cref="I3DGeometry"/> instance.
    /// </summary>
    Box boundingBox { get; set; }

    /// <summary>
    /// Gets or sets the center point for this <see cref="I3DGeometry"/> instance.
    /// </summary>
    Point center { get; set; }

    /// <summary>
    /// Gets or sets the area for this <see cref="I3DGeometry"/> instance.
    /// </summary>
    double area { get; set; }
    
    /// <summary>
    /// Gets or set the length of this <see cref="I3DGeometry"/> instance.
    /// </summary>
    double length { get; set; }
  }


  /// <summary>
  /// Used to define a curve based Geometry
  /// </summary>
  public interface ICurve : I2DGeometry
  {
  }
}