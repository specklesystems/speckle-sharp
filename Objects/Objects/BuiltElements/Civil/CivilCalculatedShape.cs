using System.Collections.Generic;
using Speckle.Core.Models;

namespace Objects.BuiltElements.Civil;

public class CivilCalculatedShape : Base, ICivilCalculatedObject
{
  public CivilCalculatedShape() { }

  public CivilCalculatedShape(List<string> codes, List<CivilCalculatedLink> links, double area, string units)
  {
    this.codes = codes;
    this.links = links;
    this.area = area;
    this.units = units;
  }

  public List<string> codes { get; set; }

  [DetachProperty]
  public List<CivilCalculatedLink> links { get; set; }

  public double area { get; set; }

  public string units { get; set; }
}
