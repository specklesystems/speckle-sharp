using System;
using System.Collections.Generic;
using Archicad.Converters;
using Objects.BuiltElements.Archicad;
using Beam = Objects.BuiltElements.Archicad.ArchicadBeam;
using Column = Objects.BuiltElements.Archicad.ArchicadColumn;
using DirectShape = Objects.BuiltElements.Archicad.DirectShape;
using Door = Objects.BuiltElements.Archicad.ArchicadDoor;
using Floor = Objects.BuiltElements.Archicad.ArchicadFloor;
using Room = Objects.BuiltElements.Archicad.ArchicadRoom;
using Wall = Objects.BuiltElements.Archicad.ArchicadWall;
using Window = Objects.BuiltElements.Archicad.ArchicadWindow;

namespace Archicad
{
  public static class ElementTypeProvider
  {
    private static Dictionary<string, Type> _nameToType = new()  {
      { "Wall", typeof(Wall) },
      { "Slab", typeof(Floor) },
      { "Zone", typeof(Room) },
      { "Beam", typeof(Beam) },
      { "Column", typeof(Column) },
      { "Door", typeof(Door) },
      { "Window", typeof(Window) }

    };

    public static Type GetTypeByName(string name)
    {
      return _nameToType.TryGetValue(name, out var value) ? value : typeof(DirectShape);
    }
  }
}
