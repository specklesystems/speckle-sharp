using System;
using System.Collections.Generic;
using System.Text;
using Objects.BuiltElements;

namespace Objects.Revit
{
  public class RevitBrace : RevitFamilyElement, IBrace
  {
    public ICurve baseLine { get; set; }

  }
}
