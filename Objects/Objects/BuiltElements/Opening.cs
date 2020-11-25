using Objects.Geometry;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Objects.BuiltElements
{
  public class Opening : IOpening
  {
    public ICurve outline { get; set; }

    public Opening()
    {

    }
  }
}
