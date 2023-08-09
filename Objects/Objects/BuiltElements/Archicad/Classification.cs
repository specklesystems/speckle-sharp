using System;
using System.Collections.Generic;
using System.Text;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.BuiltElements.Archicad
{
  public class Classification : Base
  {
    public Classification() { }

    [SchemaInfo("Classification", "A classification to set on an element", "BIM", "All")]
    public Classification(string system, string code = null, string name = null)
    {
      this.system = system;
      this.code = code;
      this.name = name;
    }

    public string system { get; set; }
    public string ?code { get; set; }
    public string ?name { get; set; }
  }
}
