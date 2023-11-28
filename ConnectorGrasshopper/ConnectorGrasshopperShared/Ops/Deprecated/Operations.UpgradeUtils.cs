using System;
using ConnectorGrasshopper.Ops.Deprecated;
using ConnectorGrasshopper.UpgradeUtilities;
using Grasshopper.Kernel;

namespace ConnectorGrasshopper.Ops;

public class UpgradeSenderToVariableInput : IGH_UpgradeObject
{
  public IGH_DocumentObject Upgrade(IGH_DocumentObject target, GH_Document document)
  {
    var component = target as IGH_Component;
    if (component == null)
    {
      return null;
    }

    var upgradedComponent = GH_UpgradeUtil.SwapComponents(component, UpgradeTo);
    var attributes = new VariableInputSendComponentAttributes(upgradedComponent as GH_Component);
    attributes.Bounds = upgradedComponent.Attributes.Bounds;
    attributes.Pivot = upgradedComponent.Attributes.Pivot;
    upgradedComponent.Attributes = attributes;
    upgradedComponent.Params.Input.Add(upgradedComponent.Params.Input[0]);
    upgradedComponent.Params.Input.RemoveAt(0);
    UpgradeUtils.SwapGroups(document, component, upgradedComponent);
    return upgradedComponent;
  }

  public DateTime Version => new(2022, 01, 10);
  public Guid UpgradeFrom => new SendComponent().ComponentGuid;
  public Guid UpgradeTo => new VariableInputSendComponent().ComponentGuid;
}

public class UpgradeReceiverToVariableOutput : IGH_UpgradeObject
{
  public IGH_DocumentObject Upgrade(IGH_DocumentObject target, GH_Document document)
  {
    var component = target as IGH_Component;
    if (component == null)
    {
      return null;
    }

    var upgradedComponent = GH_UpgradeUtil.SwapComponents(component, UpgradeTo);
    var attributes = new VariableInputReceiveComponentAttributes(upgradedComponent as GH_Component);
    attributes.Bounds = upgradedComponent.Attributes.Bounds;
    attributes.Pivot = upgradedComponent.Attributes.Pivot;
    upgradedComponent.Attributes = attributes;
    upgradedComponent.Params.Output.Add(upgradedComponent.Params.Output[0]);
    upgradedComponent.Params.Output.RemoveAt(0);
    UpgradeUtils.SwapGroups(document, component, upgradedComponent);
    return upgradedComponent;
  }

  public DateTime Version => new(2022, 01, 10);
  public Guid UpgradeFrom => new ReceiveComponent().ComponentGuid;
  public Guid UpgradeTo => new VariableInputReceiveComponent().ComponentGuid;
}

public class UpgradeSenderToNewDataTree : IGH_UpgradeObject
{
  public IGH_DocumentObject Upgrade(IGH_DocumentObject target, GH_Document document)
  {
    var component = target as IGH_Component;
    if (component == null)
    {
      return null;
    }

    var upgradedComponent = GH_UpgradeUtil.SwapComponents(component, UpgradeTo);
    var attributes = new NewVariableInputSendComponentAttributes(upgradedComponent as GH_Component);
    attributes.Bounds = upgradedComponent.Attributes.Bounds;
    attributes.Pivot = upgradedComponent.Attributes.Pivot;
    upgradedComponent.Attributes = attributes;
    UpgradeUtils.SwapGroups(document, component, upgradedComponent);
    return upgradedComponent;
  }

  public DateTime Version => new(2022, 5, 31);
  public Guid UpgradeFrom => new("6E528842-C478-4BD0-8DA6-30B7D1F08B04");
  public Guid UpgradeTo => new("B7B46BA5-DF54-4D0C-9668-7E9287409C20");
}

public class UpgradeSyncSenderToSpeckleBase : IGH_UpgradeObject
{
  public IGH_DocumentObject Upgrade(IGH_DocumentObject target, GH_Document document)
  {
    var component = target as IGH_Component;
    if (component == null)
    {
      return null;
    }

    var upgradedComponent = GH_UpgradeUtil.SwapComponents(component, UpgradeTo);
    UpgradeUtils.SwapGroups(document, component, upgradedComponent);
    return upgradedComponent;
  }

  public DateTime Version => new(2022, 6, 1);
  public Guid UpgradeFrom => new SendComponentSync().ComponentGuid;
  public Guid UpgradeTo => new SyncSendComponent().ComponentGuid;
}

public class UpgradeReceiveSenderToSpeckleBase : IGH_UpgradeObject
{
  public IGH_DocumentObject Upgrade(IGH_DocumentObject target, GH_Document document)
  {
    var component = target as IGH_Component;
    if (component == null)
    {
      return null;
    }

    var upgradedComponent = GH_UpgradeUtil.SwapComponents(component, UpgradeTo);
    UpgradeUtils.SwapGroups(document, component, upgradedComponent);
    return upgradedComponent;
  }

  public DateTime Version => new(2022, 6, 1);
  public Guid UpgradeFrom => new ReceiveSync().ComponentGuid;
  public Guid UpgradeTo => new SyncReceiveComponent().ComponentGuid;
}
