using Speckle.Objects.Geometry;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Speckle.Objects
{
  /// <summary>
  /// Used to define a geometrical Base
  /// </summary>
  public interface IGeometry
  {
  }

  /// <summary>
  /// Used to define a curve based Geometry
  /// </summary>
  public interface ICurve : IGeometry
  {
  }
}
