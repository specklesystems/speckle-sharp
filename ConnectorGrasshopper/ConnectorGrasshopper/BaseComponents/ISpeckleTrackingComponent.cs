namespace ConnectorGrasshopper
{
  public interface ISpeckleTrackingComponent
  {
    /// <summary>
    /// Common implementation for all tracking events inside a component.
    /// </summary>
    ComponentTracker Tracker { set; get; }
    /// <summary>
    /// Used to track node creation. If a node was deserialized (either by copy/paste or opening a new file) this should be set to `false`.
    /// Upon Adding to a document, we should track node creation only if IsNew = true.
    /// </summary>
    bool IsNew { get; set; }
  }
}
