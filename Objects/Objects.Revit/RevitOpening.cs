using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;

namespace Objects.Revit
{
  [SchemaIgnore]
  public class RevitOpening : Base, IRevitElement, IOpening
  {
    public string elementId { get; set; }

    public ICurve outline { get; set; }

    [SchemaOptional]
    public string family { get; set; }

    [SchemaOptional]
    public string type { get; set; }

    [SchemaOptional]
    public Dictionary<string, object> parameters { get; set; }

    [SchemaOptional]
    public Dictionary<string, object> typeParameters { get; set; }
  }

  [SchemaIgnore]
  public class RevitVerticalOpening : RevitOpening
  {
    public int revitHostId { get; set; }
  }

  [SchemaIgnore]
  public class RevitWallOpening : RevitOpening
  {
    public RevitWall host { get; set; }

    public int revitHostId { get; set; }
  }

  public class RevitShaft : RevitOpening
  {
    public RevitLevel bottomLevel { get; set; }

    public RevitLevel topLevel { get; set; }
  }
}