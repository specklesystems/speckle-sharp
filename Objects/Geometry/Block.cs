using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Speckle.Objects.Geometry
{
  public class Block : Base, IGeometry
  {
    public string description { get; set; }
    public List<Base> objects { get; set; }
    public Block()
    {

    }
  }
}
