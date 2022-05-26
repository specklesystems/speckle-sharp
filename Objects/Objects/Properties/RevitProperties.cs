using Speckle.Core.Kits;

namespace Objects.Properties
{
  public class RevitProperties : ApplicationProperties
  {
    public new string app => HostApplications.Revit.Name;
    public string family { get; set; }
    public string type { get; set; }
    public string elementId { get; set; }
  }
}