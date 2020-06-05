using Speckle.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Speckle.Elements.Simple
{
  public class Boolean : Base
  {
    public bool? value { get; set; }

    public Boolean() { }

    public Boolean(bool value)
    {
      this.value = value;
    }
  }
}
