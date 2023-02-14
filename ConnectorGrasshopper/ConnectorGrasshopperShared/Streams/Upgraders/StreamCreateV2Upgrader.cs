using System;
using ConnectorGrasshopper.UpgradeUtilities;
using Grasshopper.Kernel;

namespace ConnectorGrasshopper.Streams.Upgraders
{
  public abstract class SpeckleUpgradeObject<T1, T2> : IGH_UpgradeObject where T1 : GH_Component where T2 : GH_Component
  {
    public IGH_DocumentObject Upgrade(IGH_DocumentObject target, GH_Document document)
    {
      if (!(target is T1 component))
        return null; // Ensure the type of the target is correct

      // Upgrade the component
      var upgraded = GH_UpgradeUtil.SwapComponents(component, UpgradeTo);
      
      if (!(upgraded is T2 upgradedComponent))
        return null; // Ensure the type of the upgraded component is correct
      
      // Run any custom upgrade steps here, such as swapping access type, updating nicknames... etc.
      CustomUpgrade(component, upgradedComponent, document);
      
      // Swap the groups this node belongs to.
      UpgradeUtils.SwapGroups(document, component, upgradedComponent);
      
      return upgradedComponent;
    }
    
    public abstract void CustomUpgrade(T1 oldComponent, T2 newComponent, GH_Document document);
    
    public abstract DateTime Version { get; }

    public Guid UpgradeFrom => ((GH_Component)Activator.CreateInstance(typeof(T1))).ComponentGuid;
    public Guid UpgradeTo => ((GH_Component)Activator.CreateInstance(typeof(T2))).ComponentGuid;
  }

  public class StreamCreateV2UpgradeObject : SpeckleUpgradeObject<StreamCreateComponent, StreamCreateComponentV2>
  {
    public override void CustomUpgrade(StreamCreateComponent oldComponent, StreamCreateComponentV2 newComponent, GH_Document document)
    {
      // Nothing to do here.
    }

    public override DateTime Version => new DateTime(2023, 2, 14);
  }

  public class StreamDetailsV2UpgradeObject : SpeckleUpgradeObject<StreamDetailsComponent,StreamDetailsComponentV2>
  {
    public override void CustomUpgrade(StreamDetailsComponent oldComponent, StreamDetailsComponentV2 newComponent, GH_Document document)
    {
      throw new NotImplementedException();
    }

    public override DateTime Version => new DateTime(2023, 2, 14);
  }

  public class StreamGetV2UpgradeObject : SpeckleUpgradeObject<StreamUpdateComponent,StreamUpdateComponentV2>
  {
    public override void CustomUpgrade(StreamUpdateComponent oldComponent, StreamUpdateComponentV2 newComponent, GH_Document document)
    {
      throw new NotImplementedException();
    }

    public override DateTime Version => new DateTime(2023, 2, 14);
  }

  public class StreamListV2UpgradeObject : SpeckleUpgradeObject<StreamListComponent,StreamListComponentV2>
  {
    public override void CustomUpgrade(StreamListComponent oldComponent, StreamListComponentV2 newComponent, GH_Document document)
    {
      throw new NotImplementedException();
    }

    public override DateTime Version => new DateTime(2023, 2, 14);
  }

  public class StreamUpdateV2UpgradeObject : SpeckleUpgradeObject<StreamUpdateComponent,StreamUpdateComponentV2>
  {
    public override void CustomUpgrade(StreamUpdateComponent oldComponent, StreamUpdateComponentV2 newComponent, GH_Document document)
    {
      throw new NotImplementedException();
    }

    public override DateTime Version => new DateTime(2023, 2, 14);
  }
}
