using System;
using System.Collections.Generic;
using System.Text;
using Speckle.Core.Models;

namespace Objects.ProjectOrganization
{
  public class Level:Base
  {
    public string name { get; set; }
    public double referenceElevation { get; set; }
    public double elevation { get; set; }
  }
}
