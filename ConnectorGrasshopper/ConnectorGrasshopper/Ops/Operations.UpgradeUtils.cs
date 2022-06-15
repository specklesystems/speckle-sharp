﻿using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using ConnectorGrasshopper.UpgradeUtilities;

namespace ConnectorGrasshopper.Ops
{
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

    public DateTime Version => new DateTime(2022, 01, 10);
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

    public DateTime Version => new DateTime(2022, 01, 10);
    public Guid UpgradeFrom => new ReceiveComponent().ComponentGuid;
    public Guid UpgradeTo => new VariableInputReceiveComponent().ComponentGuid;
  }
}
