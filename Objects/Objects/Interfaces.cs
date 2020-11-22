using Objects.BuiltElements;
using Objects.Geometry;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Objects
{
  #region geometry
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

  #endregion

  #region builtelems

  public interface IBuiltElement
  {
    string applicationId { get; set; }
    string speckle_type { get; }
  }

  public interface IBeam : IBuiltElement
  {
    ICurve baseLine { get; set; }

  }

  public interface IBrace : IBuiltElement
  {
    ICurve baseLine { get; set; }

  }

  public interface IColumn : IBuiltElement
  {
    double height { get; set; }

    ICurve baseLine { get; set; }

  }

  public interface IDuct : IBuiltElement
  {
    double width { get; set; }
    double height { get; set; }
    double diameter { get; set; }
    double length { get; set; }
    double velocity { get; set; }

    Line baseLine { get; set; }

  }

  public interface IFloor : IBuiltElement
  {
    ICurve outline { get; set; }
    List<ICurve> voids { get; set; }
  }

  public interface IGridLine : IBuiltElement
  {
    Line baseLine { get; set; }
  }

  public interface ILevel : IBuiltElement
  {
    string name { get; set; }
    double elevation { get; set; }
    List<Element> elements { get; set; }
  }

  public interface IOpening : IBuiltElement
  {
    ICurve outline { get; set; }
  }

  public interface IRoof : IBuiltElement
  {
    ICurve outline { get; set; }
    List<ICurve> voids { get; set; }
  }

  public interface IRoom : IBuiltElement
  {
    string name { get; set; }
    string number { get; set; }
    double area { get; set; }
    double volume { get; set; }
    List<ICurve> voids { get; set; }
    ICurve outline { get; set; }
  }

  public interface ITopography : IBuiltElement
  {
    Mesh baseGeometry { get; set; }

  }

  public interface IWall : IBuiltElement
  {
    double height { get; set; }

    ICurve baseLine { get; set; }


  }

  #endregion
}
