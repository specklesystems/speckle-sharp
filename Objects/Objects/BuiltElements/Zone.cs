using System.Collections.Generic;
using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.BuiltElements
{
  public class Zone : Base, IHasArea, IHasVolume
  {
    public Zone() { }
    
    public Zone(string name)
    {
      this.name = name;
    }

    public string name { get; set; }
    public string units { get; set; }
    
    public List<Space> spaces { get; set; }
    
    // implicit measurements
    public double area { get; set; }
    public double volume { get; set; }
    public double perimeter { get; set; }
  }
}

namespace Objects.BuiltElements.Revit
{
  public class RevitZone : Zone
  {
    public RevitZone() { }

    public Level level { get; set; }
    public string phaseName { get; set; }
    public Base parameters { get; set; }
    public string elementId { get; set; }
    public bool isDefault { get; set; }
    public string serviceType { get; set; }
  }
}
