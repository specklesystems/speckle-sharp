using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;

namespace Objects.BuiltElements.TeklaStructures
{
  public class TeklaModel : Base
  {
    [DetachProperty]
    public List<Base> Beams { get; set; }

    [DetachProperty]

    public List<Base> Rebars { get; set; }
  }
}
