using System;
using ConnectorGrasshopper.UpgradeUtilities;
using Grasshopper.Kernel;

namespace ConnectorGrasshopper.Conversion
{
  public class Upgrade_ConvertToNative : IGH_UpgradeObject
  {
    public IGH_DocumentObject Upgrade(IGH_DocumentObject target, GH_Document document)
    {
      if (!(target is IGH_Component component))
        return null;

      var upgradedComponent = GH_UpgradeUtil.SwapComponents(component, UpgradeTo);
      upgradedComponent.Params.Input[0].Access = GH_ParamAccess.item;
      upgradedComponent.Params.Output[0].Access = GH_ParamAccess.item;
      UpgradeUtils.SwapGroups(document, component, upgradedComponent);
      return upgradedComponent;
    }

    public DateTime Version => new DateTime(2021, 12, 12);
    public Guid UpgradeFrom => new ToNativeConverterAsync().ComponentGuid;
    public Guid UpgradeTo => new ToNativeTaskCapableComponent().ComponentGuid;
  }

  public class Upgrade_ConvertToSpeckle : IGH_UpgradeObject
  {
    public IGH_DocumentObject Upgrade(IGH_DocumentObject target, GH_Document document)
    {
      if (!(target is IGH_Component component))
        return null;

      var upgradedComponent = GH_UpgradeUtil.SwapComponents(component, UpgradeTo);
      
      upgradedComponent.Params.Input[0].Access = GH_ParamAccess.item;
      upgradedComponent.Params.Output[0].Access = GH_ParamAccess.item;
      UpgradeUtils.SwapGroups(document, component, upgradedComponent);
      return upgradedComponent;
    }

    public DateTime Version => new DateTime(2021, 12, 12);
    public Guid UpgradeFrom => new ToSpeckleConverterAsync().ComponentGuid;
    public Guid UpgradeTo => new ToSpeckleTaskCapableComponent().ComponentGuid;
  }

  public class Upgrade_SerialiseAsyncComponent : IGH_UpgradeObject
  {
    public IGH_DocumentObject Upgrade(IGH_DocumentObject target, GH_Document document)
    {
      if (!(target is IGH_Component component))
        return null;

      var upgradedComponent = GH_UpgradeUtil.SwapComponents(component, UpgradeTo);
      
      upgradedComponent.Params.Input[0].Access = GH_ParamAccess.item;
      upgradedComponent.Params.Output[0].Access = GH_ParamAccess.item;
      UpgradeUtils.SwapGroups(document, component, upgradedComponent);
      return upgradedComponent;
    }

    public DateTime Version => new DateTime(2021, 12, 12);
    public Guid UpgradeFrom => new SerializeObject().ComponentGuid;
    public Guid UpgradeTo => new SerializeTaskCapableComponent().ComponentGuid;
    
  }
  
  public class Upgrade_DeserialiseAsyncComponent : IGH_UpgradeObject
  {
    public IGH_DocumentObject Upgrade(IGH_DocumentObject target, GH_Document document)
    {
      if (!(target is IGH_Component component))
        return null;

      var upgradedComponent = GH_UpgradeUtil.SwapComponents(component, UpgradeTo);
      
      upgradedComponent.Params.Input[0].Access = GH_ParamAccess.item;
      upgradedComponent.Params.Output[0].Access = GH_ParamAccess.item;
      
      UpgradeUtils.SwapGroups(document, component, upgradedComponent);
      return upgradedComponent;
    }

    public DateTime Version => new DateTime(2021, 12, 12);
    public Guid UpgradeFrom => new DeserializeObject().ComponentGuid;
    public Guid UpgradeTo => new DeserializeTaskCapableComponent().ComponentGuid;
    
  }
}
