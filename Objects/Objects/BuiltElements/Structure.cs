using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Newtonsoft.Json;

namespace Objects.BuiltElements
{
  public class Structure : Base, IDisplayValue<List<Mesh>>
  {
    public Point location { get; set; }
    public List<string> pipeIds { get; set; }

    [DetachProperty]
    public List<Mesh> displayValue { get; set; }

    public string units { get; set; }

    public Structure() { }

  }
}
