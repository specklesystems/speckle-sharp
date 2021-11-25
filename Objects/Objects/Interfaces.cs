using Objects.BuiltElements;
using Objects.Geometry;
using Objects.Other;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using Objects.Primitive;

namespace Objects
{
  #region Generic interfaces.

  public interface IHasBoundingBox
  {
    Box bbox { get; }
  }

  public interface IHasArea
  {
    double area { get; set; }
  }

  public interface IHasVolume
  {
    double volume { get; set; }
  }

  public interface ICurve   
  {
    double length { get; set; }
    Interval domain { get; set; }
  }

  public interface ITransformable<T> where T : ITransformable<T>
  {
    T Transform(Transform transform);
  }

  #endregion

  #region Built elements

  public interface IDisplayMesh
  {
    Mesh displayMesh { get; set; }
  }



  #endregion
}
