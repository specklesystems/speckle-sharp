using DesktopUI2.Models.Filters;
using Speckle.ConnectorNavisworks.Objects;
using Speckle.Core.Kits;
using System;
using System.Collections.Generic;

namespace Speckle.ConnectorNavisworks.Bindings
{
  public partial class ConnectorBindingsNavisworks
  {
    public override List<string> GetObjectsInView() // this returns all visible doc objects.
    {
      var objects = new List<string>();
      return objects;
    }

    private List<ConvertableObject> GetObjectsFromFilter(ISelectionFilter filter)
    {
      var objs = new List<ConvertableObject>();

      return objs;
    }
  }
}