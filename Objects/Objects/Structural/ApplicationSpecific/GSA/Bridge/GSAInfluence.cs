using Speckle.Core.Models;
using Speckle.Core.Kits;
using System.Collections.Generic;
using Objects.Structural.Loading;

namespace Objects.Structural.GSA.Bridge
{
  public class GSAInfluence : Base
  {
    public int nativeId { get; set; }
    public string name { get; set; }
    public double factor { get; set; }
    public InfluenceType type { get; set; }
    public LoadDirection direction { get; set; }
    public GSAInfluence() { }

    public GSAInfluence(int nativeId, string name, double factor, InfluenceType type, LoadDirection direction)
    {
      this.nativeId = nativeId;
      this.name = name;
      this.factor = factor;
      this.type = type;
      this.direction = direction;
    }
  }

  public enum InfluenceType
  {
    NotSet = 0,
    FORCE,
    DISPLACEMENT
  }
}
