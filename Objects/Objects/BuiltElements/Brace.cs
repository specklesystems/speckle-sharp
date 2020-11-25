using Objects.Geometry;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Objects.BuiltElements
{
  public class Brace : IBrace
  {
    public ICurve baseLine { get; set; }
    public Brace()
    {

    }
  }
}
