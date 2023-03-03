using System;
using System.Collections.Generic;
using System.Text;
using Speckle.Core.Models;
using Objects.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Speckle.Newtonsoft.Json;

namespace Objects.BuiltElements.AdvanceSteel
{
  public abstract class AdvanceSteelBolt : Base
  {
    [DetachProperty]
    public List<Mesh> displayValue { get; set; }
  }

  public class AdvanceSteelCircularBolt : AdvanceSteelBolt
  {

  }

  public class AdvanceSteelRectangularBolt : AdvanceSteelBolt
  {

  }
}
