using Speckle.Newtonsoft.Json;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;
using Objects.Geometry;
using Objects.Structural.Properties;
using Objects.Structural.Properties.Profiles;
using Objects.Structural.Materials;
using Objects.Structural.Geometry;
using Objects.Structural.Loading;

//Place holder for classes prior to being added to the structural schema

namespace Objects.Structural.GSA.Other
{
  public class GSAGridLine : Base
  {
    public string name { get; set; }
    public int nativeId { get; set; }
    public Base line { get; set; }
    public GSAGridLine() { }
  }

  public class GSAGridPlane : Base
  {
    public string name { get; set; }
    public int nativeId { get; set; }
    public Axis axis { get; set; }
    public double elevation { get; set; }
    public string storeyToleranceBelow { get; set; }
    public string storeyToleranceAbove { get; set; }
    public GSAGridPlane() { }
  }

  public class GSAGridSurface : Base
  {
    public string name { get; set; }
    public int nativeId { get; set; }
    public GSAGridPlane gridPlane { get; set; }
    public double tolerance { get; set; }
    public double spanDirection { get; set; }
    public LoadExpansion loadExpansion { get; set; }
    public GridSurfaceSpanType span { get; set; }
    public List<Base> elements { get; set; }
    public GSAGridSurface() { }
  }

  public enum GridSurfaceSpanType
  {
    NotSet = 0,
    OneWay,
    TwoWay
  }

  public enum LoadExpansion
  {
    NotSet = 0,
    Legacy = 1,
    PlaneAspect = 2,
    PlaneSmooth = 3,
    PlaneCorner = 4
  }

  public abstract class GSAGridLoad : Load
  {
    public int nativeId { get; set; }
    public GSAGridSurface gridSurface { get; set; }
    public Axis loadAxis { get; set; }
    public LoadDirection direction { get; set; }
  }

  public class GSAGridPointLoad : GSAGridLoad
  {
    public Point position { get; set; }
    public double value { get; set; }
    public GSAGridPointLoad() { }
  }

  public class GSAGridLineLoad : GSAGridLoad
  {
    public Polyline polyline { get; set; }
    public bool isProjected { get; set; }
    public List<double> values { get; set; }
    public GSAGridLineLoad() { }
  }

  public class GSAGridAreaLoad : GSAGridLoad
  {
    public Polyline polyline { get; set; }
    public bool isProjected { get; set; }
    public double value { get; set; }
    public GSAGridAreaLoad() { }
  }

  public class GSAThermal2dLoad : Load
  {
    public int nativeId { get; set; }
    public List<Element2D> elements { get; set; }
    public Thermal2dLoadType type { get; set; }
    public List<double> values { get; set; }
    public GSAThermal2dLoad() { }
  }

  public enum Thermal2dLoadType
  {
    NotSet = 0,
    Uniform,
    Gradient,
    General
  }

  public class GSAPolyline : Polyline
  {
    public string name { get; set; }
    public int nativeId { get; set; }
    public string colour { get; set; }
    public GSAGridPlane gridPlane { get; set; }
    public GSAPolyline() { }
  }
}
