namespace DUI3.Objects;

//Operations.Receive gives us Base object --> Creates speckle base objects into C# base objects
//Traverse function to get Traversal Context maps Base -> TraversalContext (with parental hierarchy)
//Map Traversal contexts to ConversionContexts + ??Reporting object??
//Perform conversion - might need to create more report objects on the fly as we convert (for Instances)
//Map conversion result to SpeckleHostObject

// result of a ToNative conversion...
public abstract class SpeckleHostObject<T> : ISpeckleHostObject
{
  public virtual T NativeObject { get; }
  public string ApplicationId { get; }
  public string SpeckleId { get; }
  public bool IsExpired { get; }

  public abstract SpeckleHostObject<T> WithExpiredStatus(bool status = true);
}
