using Speckle.Elements.Geometry;
using Speckle.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Speckle.Elements
{
  public class Column : Element
  {
    public Level topLevel { get; set; }
    public double height { get; set; }

    public Column()
    {

    }
  }
}
