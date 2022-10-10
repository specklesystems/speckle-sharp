using System;
using System.Collections.Generic;
using Archicad.Converters;
using Objects.BuiltElements.Archicad;
using DirectShape = Objects.BuiltElements.Archicad.DirectShape;
using Floor = Objects.BuiltElements.Archicad.Floor;
using Room = Objects.BuiltElements.Archicad.Room;
using Wall = Objects.BuiltElements.Archicad.Wall;
using Beam = Objects.BuiltElements.Archicad.ArchicadBeam;

namespace Archicad
{
  public static class ElementTypeProvider
  {
    private static Dictionary<string, Type> _nameToType = new()  {
      { "Wall", typeof(Wall) },
      { "Slab", typeof(Floor) },
      { "Zone", typeof(Room) },
      { "Beam", typeof(Beam) }
    };

    public static Type GetTypeByName(string name)
    {
      return _nameToType.TryGetValue(name, out var value) ? value : typeof(DirectShape);
    }
  }
}
