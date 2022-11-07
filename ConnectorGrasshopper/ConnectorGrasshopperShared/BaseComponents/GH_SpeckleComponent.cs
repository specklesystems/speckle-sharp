using System.Collections.Generic;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;

namespace ConnectorGrasshopper
{
  /// <summary>
  /// Base implementation for all Speckle nodes.
  /// INFO: Any changes here should be mirrored in GH_SpeckleTaskCapableComponent and GH_SpeckleAsyncComponent
  /// </summary>
  public abstract class GH_SpeckleComponent: GH_Component, ISpeckleTrackingComponent
  {
    public ComponentTracker Tracker { get; set; }
    public bool IsNew { get; set; } = true;
    
    protected GH_SpeckleComponent(string name, string nickname, string description, string category, string subCategory) : base(name, nickname, description, category, subCategory)
    {
      Tracker = new ComponentTracker(this);
    }

    public override bool Read(GH_IReader reader)
    {
      // Set isNew to false, indicating this node already existed in some way. This prevents the `NodeCreate` event from being raised.
      IsNew = false;
      return base.Read(reader);
    }

    public override void AddedToDocument(GH_Document document)
    {
      base.AddedToDocument(document);
      // If the node is new (i.e. GH has not called Read(...) ) we log the node creation event.
      if (IsNew)
      {
        Tracker.TrackNodeCreation();
        IsNew = false;
      }
    }
  }
}
