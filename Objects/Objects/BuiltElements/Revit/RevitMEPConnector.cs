using System.Collections.Generic;
using System.Linq;
using Objects.Geometry;
using Objects.Organization;
using Speckle.Core.Models;

namespace Objects.BuiltElements.Revit
{
  public class RevitMEPConnector : Base
  {
    public Point Origin { get; set; }
    public List<string> ConnectedConnectorIds { get; set; } = new();
    public double Angle { get; set; }
    public string HostApplicationId => this.applicationId.Split('.').First();
    public int HostConnectorNum => int.Parse(this.applicationId.Split('.').Last());
  }
}
