using Grasshopper.Kernel;

namespace ConnectorGrasshopper
{
  public interface ISpeckleTrackingDocumentObject
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

  public interface ISpeckleTrackingComponent
  {
    /// <summary>
    /// This is where the logic of of SolveInstance should be implemented.
    /// Solve Instance should call `SolveInstanceWithLogContext` wrapped in a using statement that provides all necessary metadata for that run.
    /// </summary>
    /// <param name="DA">The solve instance DA input</param>
    void SolveInstanceWithLogContext(IGH_DataAccess DA);
  }
}
