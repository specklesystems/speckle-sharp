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
    string linearUnits { get; set; }
  }

  public interface I3DGeometry : IGeometry
  {
    Box boundingBox { get; set; }

    Point center { get; set; }

    double volume { get; set; }

    double area { get; set; }

  }

  public interface I2DGeometry : IGeometry
  {
    Box boundingBox { get; set; }

    Point center { get; set; }

    double area { get; set; }

    double length { get; set; }
  }



  /// <summary>
  /// Used to define a curve based Geometry
  /// </summary>
  public interface ICurve : I2DGeometry
  {
  }
}
