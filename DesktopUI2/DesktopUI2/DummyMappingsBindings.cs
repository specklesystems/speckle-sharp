using Objects.Geometry;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DesktopUI2
{
  public class DummyMappingsBindings : MappingsBindings
  {
    public override void SetMappings()
    {
      throw new NotImplementedException();
    }

    public override List<Base> GetSelection()
    {
      return new List<Base> { new Line(), new Line(), new Line(), new Line(), new Line() };
    }

    public DummyMappingsBindings()
    {

    }
  }
}
