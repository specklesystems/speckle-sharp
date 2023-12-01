using DUI3.Objects;
using Rhino.DocObjects;

namespace ConnectorRhinoWebUI.Utils;

// We get from conversion (result of ToNative)
// Information the Connector needs to track objects
public class SpeckleRhinoObject : SpeckleHostObject<RhinoObject>
{
  public override RhinoObject NativeObject { get; }
  public new string ApplicationId { get; }
  public new string SpeckleId { get; }
  public new bool IsExpired { get; }

  public SpeckleRhinoObject(RhinoObject rhinoObject, string applicationId, string speckleId, bool isExpired = false)
  {
    NativeObject = rhinoObject;
    ApplicationId = applicationId;
    SpeckleId = speckleId;
    IsExpired = isExpired;
  }

  public override SpeckleHostObject<RhinoObject> WithExpiredStatus(bool status = true)
  {
    return new SpeckleRhinoObject(this.NativeObject, this.ApplicationId, this.SpeckleId, status);
  }
}
