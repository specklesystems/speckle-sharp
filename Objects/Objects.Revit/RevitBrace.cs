using System;
using System.Collections.Generic;
using System.Text;
using Objects.BuiltElements;
using Speckle.Core.Kits;

namespace Objects.Revit
{
  [SchemaDescription("A Revit brace by line")]
  public class RevitBrace : RevitElement, IBrace
  {
    public ICurve baseLine { get; set; }

  }
}
