using System.Collections.Generic;
using Speckle.Core.Models;

namespace Objects.BuiltElements.TeklaStructures;

public class TeklaModel : Base
{
  [DetachProperty]
  public List<Base> Beams { get; set; }

  [DetachProperty]
  public List<Base> Rebars { get; set; }
}
