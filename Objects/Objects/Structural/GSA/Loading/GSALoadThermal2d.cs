using System.Collections.Generic;
using Objects.Structural.Geometry;
using Objects.Structural.Loading;
using Speckle.Core.Models;

namespace Objects.Structural.GSA.Loading;

public class GSALoadThermal2d : Load
{
  public GSALoadThermal2d() { }

  public GSALoadThermal2d(int nativeId, List<Element2D> elements, Thermal2dLoadType type, List<double> values)
  {
    this.nativeId = nativeId;
    this.elements = elements;
    this.type = type;
    this.values = values;
  }

  public int nativeId { get; set; }

  [DetachProperty, Chunkable(5000)]
  public List<Element2D> elements { get; set; }

  public Thermal2dLoadType type { get; set; }
  public List<double> values { get; set; }
}

public enum Thermal2dLoadType
{
  NotSet = 0,
  Uniform,
  Gradient,
  General
}
