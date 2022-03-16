using System.Collections.Generic;
using Objects.Geometry;
using Speckle.Core.Models;
using Speckle.Newtonsoft.Json;

namespace Objects.BuiltElements.Archicad
{
  public class Room : BuiltElements.Room
  {
    public int? floorIndex { get; set; }

    public double height { get; set; }

    public ElementShape shape { get; set; }

    public Room() { }
  }
}
