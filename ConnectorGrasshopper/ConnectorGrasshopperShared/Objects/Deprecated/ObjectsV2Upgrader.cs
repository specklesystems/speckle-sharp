using System;
using ConnectorGrasshopper.UpgradeUtilities;
using Grasshopper.Kernel;

#pragma warning disable CS0612

namespace ConnectorGrasshopper.Objects;

public class CreateSpeckleObjectByKeyValueV2UpgradeObject : IGH_UpgradeObject
{
  public IGH_DocumentObject Upgrade(IGH_DocumentObject target, GH_Document document)
  {
    if (!(target is CreateSpeckleObjectByKeyValueTaskComponent component))
      return null; // Ensure the type of the target is correct

    // Upgrade the component
    var upgraded = GH_UpgradeUtil.SwapComponents(component, UpgradeTo);

    if (!(upgraded is CreateSpeckleObjectByKeyValueV2TaskComponent upgradedComponent))
      return null; // Ensure the type of the upgraded component is correct

    // Swap the groups this node belongs to.
    UpgradeUtils.SwapGroups(document, component, upgradedComponent);

    upgradedComponent.Params.Input[0].Access = GH_ParamAccess.list;
    upgradedComponent.Params.Input[1].Access = GH_ParamAccess.list;

    return upgradedComponent;
  }

  public DateTime Version => new(2023, 3, 23);
  public Guid UpgradeFrom => new CreateSpeckleObjectByKeyValueTaskComponent().ComponentGuid;
  public Guid UpgradeTo => new CreateSpeckleObjectByKeyValueV2TaskComponent().ComponentGuid;
}

public class ExtendSpeckleObjectByKeyValueV2UpgradeObject : IGH_UpgradeObject
{
  public IGH_DocumentObject Upgrade(IGH_DocumentObject target, GH_Document document)
  {
    if (!(target is ExtendSpeckleObjectByKeyValueTaskComponent component))
      return null; // Ensure the type of the target is correct

    // Upgrade the component
    var upgraded = GH_UpgradeUtil.SwapComponents(component, UpgradeTo);

    if (!(upgraded is ExtendSpeckleObjectByKeyValueV2TaskComponent upgradedComponent))
      return null; // Ensure the type of the upgraded component is correct

    // Swap the groups this node belongs to.
    UpgradeUtils.SwapGroups(document, component, upgradedComponent);

    upgradedComponent.Params.Input[1].Access = GH_ParamAccess.list;
    upgradedComponent.Params.Input[2].Access = GH_ParamAccess.list;

    return upgradedComponent;
  }

  public DateTime Version => new(2023, 3, 23);
  public Guid UpgradeFrom => new ExtendSpeckleObjectByKeyValueTaskComponent().ComponentGuid;
  public Guid UpgradeTo => new ExtendSpeckleObjectByKeyValueV2TaskComponent().ComponentGuid;
}
