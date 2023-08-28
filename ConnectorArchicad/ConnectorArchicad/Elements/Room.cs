using Objects.Geometry;
using System.Collections.Generic;
using Objects.BuiltElements.Archicad;

namespace Archicad
{
  public class Room
  {
    // Speckle-specific properties
    // Base
    public string? id { get; set; }
    public string? applicationId { get; set; }

    // General
    public string? name { get; set; }
    public string? number { get; set; }

    public double? area { get; set; }
    public double? volume { get; set; }

    // Helper
    public Point? basePoint { get; set; } // Archicad geometry kernel needed for calculation

    // Archicad API properties
    // Element base
    public string? elementType { get; set; }
    public List<Classification>? classifications { get; set; }
    public ArchicadLevel? level { get; set; }

    // Room
    public double? height { get; set; }

    public ElementShape? shape { get; set; }

    public Room() { }

    public Room(string id, string applicationId)
    {
      this.id = id;
      this.applicationId = applicationId;
    }
  }
}
