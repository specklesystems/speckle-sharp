using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.Revit
{
  [SchemaIgnore]
  public class RevitOpening : Base, IBaseRevitElement, IOpening
  {
    public string elementId { get; set; }

    public ICurve outline { get; set; }
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