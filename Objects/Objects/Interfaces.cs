using Objects.BuiltElements;
using Objects.Geometry;
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

  #endregion

  #region Built elements

  //public interface IBeam 
  //{
  //  ICurve baseLine { get; set; }
  //}

  //public interface IBrace 
  //{
  //  ICurve baseLine { get; set; }
  //}

  //public interface IColumn 
  //{
  //  double height { get; set; }

  //  ICurve baseLine { get; set; }
  //}

  //public interface IDuct 
  //{
  //  double width { get; set; }
  //  double height { get; set; }
  //  double diameter { get; set; }
  //  double length { get; set; }
  //  double velocity { get; set; }

  //  Line baseLine { get; set; }
  //}

  //public interface IFloor  
  //{
  //  ICurve outline { get; set; }
  //  List<ICurve> voids { get; set; }
  //}

  //public interface IGridLine  
  //{
  //  Line baseLine { get; set; }
  //}

  //public interface ILevel  
  //{
  //  string name { get; set; }
  //  double elevation { get; set; }
  //}

  //public interface IOpening  
  //{
  //  ICurve outline { get; set; }
  //}

  //public interface IRoof  
  //{
  //  ICurve outline { get; set; }
  //  List<ICurve> voids { get; set; }
  //}

  //public interface IRoom  
  //{
  //  string name { get; set; }
  //  string number { get; set; }
  //  double area { get; set; }
  //  double volume { get; set; }
  //  List<ICurve> voids { get; set; }
  //  ICurve outline { get; set; }
  //}

  //public interface ITopography  
  //{
  //  Mesh baseGeometry { get; set; }
  //}

  //public interface IWall  
  //{
  //  double height { get; set; }

  //  ICurve baseLine { get; set; }
  //}

  #endregion
}
