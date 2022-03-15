using System;
using System.Collections.Generic;
using Archicad.Converters;
using Objects.BuiltElements.Archicad;
using Ceiling = Objects.BuiltElements.Archicad.Ceiling;
using DirectShape = Objects.BuiltElements.Archicad.DirectShape;
using Wall = Objects.BuiltElements.Archicad.Wall;

namespace Archicad
{
  public static class ElementTypeProvider
  {
    private static Dictionary<string, Type> _nameToType = new()  { { "Wall", typeof(Wall) }, { "Slab", typeof(Ceiling) }, {"Zone", typeof(Zone)} };

    public static Type GetTypeByName(string name)
    {
      return _nameToType.TryGetValue(name, out var value) ? value : typeof(DirectShape);
    }
  }
}
