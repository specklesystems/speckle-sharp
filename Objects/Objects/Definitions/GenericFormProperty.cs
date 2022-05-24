using System;
using System.Collections.Generic;
using System.Text;
using Objects.Building.enums;
using Speckle.Core.Models;

namespace Objects.Definitions
{
  public class GenericFormProperty : Base
  {
    public string name { get; set; }
    public GenericFormType Type { get; set; }
  }
}
