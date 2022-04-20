using Objects.Geometry;
using Objects.Structural.Materials;
using Objects.Structural.Properties.Profiles;
using Objects.Utils;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Speckle.Newtonsoft.Json;

namespace Objects.DefaultBuildingObjectKit.GenericElements
{
  public class Element1D : Base
  {
    public ICurve baseLine { get; set; }

    public string name { get; set; }

  }
}
