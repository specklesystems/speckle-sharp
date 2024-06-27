using Rhino;

namespace Speckle.Connectors.Rhino7.HostApp;

public class RhinoContext
{
  public RhinoDoc Document { get; } = RhinoDoc.ActiveDoc;
}
