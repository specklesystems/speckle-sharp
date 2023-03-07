using System;
using System;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text;
using Objects.Geometry;
using Speckle.Core.Models;
using Speckle.Newtonsoft.Json;

namespace Objects.BuiltElements.AdvanceSteel
{
  public abstract class AdvanceSteelBolt : Base
  {
    [DetachProperty]
    public List<Mesh> displayValue { get; set; }
    public AdvanceSteelBolt()
    {

    }
  }

  public class AdvanceSteelCircularBolt : AdvanceSteelBolt
  {
    public AdvanceSteelCircularBolt()
    {

    }
  }

  public class AdvanceSteelRectangularBolt : AdvanceSteelBolt
  {
    public AdvanceSteelRectangularBolt()
    {

    }
  }
}
