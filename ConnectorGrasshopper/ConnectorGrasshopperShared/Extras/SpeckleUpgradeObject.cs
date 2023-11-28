using System;
using ConnectorGrasshopper.UpgradeUtilities;
using Grasshopper.Kernel;

namespace ConnectorGrasshopper.Extras;

public abstract class SpeckleUpgradeObject<T1, T2> : IGH_UpgradeObject
  where T1 : GH_Component
  where T2 : GH_Component
{
  public virtual IGH_DocumentObject Upgrade(IGH_DocumentObject target, GH_Document document)
  {
    if (!(target is T1 component))
    {
      return null; // Ensure the type of the target is correct
    }

    // Upgrade the component
    var upgraded = GH_UpgradeUtil.SwapComponents(component, UpgradeTo);

    if (!(upgraded is T2 upgradedComponent))
    {
      return null; // Ensure the type of the upgraded component is correct
    }

    // Run any custom upgrade steps here, such as swapping access type, updating nicknames... etc.
    CustomUpgrade(component, upgradedComponent, document);

    // Swap the groups this node belongs to.
    UpgradeUtils.SwapGroups(document, component, upgradedComponent);

    return upgradedComponent;
  }

  public abstract DateTime Version { get; }

  public virtual Guid UpgradeFrom => ((GH_Component)Activator.CreateInstance(typeof(T1))).ComponentGuid;
  public virtual Guid UpgradeTo => ((GH_Component)Activator.CreateInstance(typeof(T2))).ComponentGuid;

  public abstract void CustomUpgrade(T1 oldComponent, T2 newComponent, GH_Document document);
}
