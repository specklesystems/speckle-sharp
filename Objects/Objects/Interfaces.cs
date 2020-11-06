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
    string linearUnits { get; set; } // all
  }

  public interface I3DGeometry : IGeometry
  {
    Box boundingBox { get; set; } // 2d & 3d stuff

    Point center { get; set; } // 2d & 3d stuff

    double volume { get; set; } // 3d-ish stuff

    double area { get; set; } // 2d & 3d stuff

  }

  public interface I2DGeometry : IGeometry
  {
    Box boundingBox { get; set; } // 2d & 3d stuff

    Point center { get; set; } // 2d & 3d stuff

    double area { get; set; } // 2d & 3d stuff

    double length { get; set; } // 2d stuff (linear)
  }



  /// <summary>
  /// Used to define a curve based Geometry
  /// </summary>
  public interface ICurve : I2DGeometry
  {
  }
}
