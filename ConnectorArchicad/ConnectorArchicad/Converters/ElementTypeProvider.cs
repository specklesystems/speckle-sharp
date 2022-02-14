using System;
using System.Collections.Generic;
using Objects.BuiltElements.Archicad;

namespace Archicad
{
  public static class ElementTypeProvider
  {
    private static Dictionary<string, Type> _nameToType = new()  { { "Wall", typeof(Wall) }, { "Slab", typeof(Ceiling) } };

    public static Type GetTypeByName(string name)
    {
      return _nameToType.TryGetValue(name, out var value) ? value : typeof(DirectShape);
    }
  }
}
