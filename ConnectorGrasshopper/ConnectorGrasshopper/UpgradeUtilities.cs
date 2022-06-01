using System;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using ConnectorGrasshopper.UpgradeUtilities;

namespace ConnectorGrasshopper.UpgradeUtilities
{
  public static class UpgradeUtils
  {
    public static void SwapGroups(GH_Document document, IGH_Component component, IGH_Component upgradedComponent)
    {
      var groups = document
        .Objects
        .OfType<GH_Group>()
        .Where(gr => gr.ObjectIDs.Contains(component.InstanceGuid))
        .ToList();
      groups.ForEach(g => g.AddObject(upgradedComponent.InstanceGuid));
    }
  }
}

namespace ConnectorGrasshopper.Objects
{
  public class Upgrade_CreateKeyValueAsyncComponent : IGH_UpgradeObject
  {
    public IGH_DocumentObject Upgrade(IGH_DocumentObject target, GH_Document document)
    {
      IGH_Component component = target as IGH_Component;
      if (component == null)
      {
        return null;
      }

      IGH_Component upgradedComponent = GH_UpgradeUtil.SwapComponents(component, UpgradeTo);
      UpgradeUtils.SwapGroups(document, component, upgradedComponent);

      return upgradedComponent;
    }

    public DateTime Version => new DateTime(2021, 6, 20);
    public Guid UpgradeFrom => new Guid("C8D4DBEB-7CC5-45C0-AF5D-F374FA5DBFBB");
    public Guid UpgradeTo => new Guid("B5232BF7-7014-4F10-8716-C3CEE6A54E2F");
  }

  public class Upgrade_CreateObjectAsyncComponent : IGH_UpgradeObject
  {
    public IGH_DocumentObject Upgrade(IGH_DocumentObject target, GH_Document document)
    {
      IGH_Component component = target as IGH_Component;
      if (component == null)
      {
        return null;
      }

      IGH_Component upgradedComponent = GH_UpgradeUtil.SwapComponents(component, UpgradeTo);
      UpgradeUtils.SwapGroups(document, component, upgradedComponent);

      return upgradedComponent;
    }

    public DateTime Version => new DateTime(2021, 6, 20);
    public Guid UpgradeFrom => new Guid("FC2EF86F-2C12-4DC2-B216-33BFA409A0FC");
    public Guid UpgradeTo => new Guid("DC561A9D-BF12-4EB3-8412-4B7FC6ECB291");
  }

  public class Upgrade_ExpandObjectAsyncComponent : IGH_UpgradeObject
  {
    public IGH_DocumentObject Upgrade(IGH_DocumentObject target, GH_Document document)
    {
      var component = target as IGH_Component;
      if (component == null)
      {
        return null;
      }

      var upgradedComponent = GH_UpgradeUtil.SwapComponents(component, UpgradeTo);
      upgradedComponent.Params.Input[0].Access = GH_ParamAccess.item;
      UpgradeUtils.SwapGroups(document, component, upgradedComponent);
      return upgradedComponent;
    }

    public DateTime Version => new DateTime(2021, 6, 20);
    public Guid UpgradeFrom => new Guid("A33BB8DF-A9C1-4CD1-855F-D6A8B277102B");
    public Guid UpgradeTo => new Guid("4884856A-BCA4-43F8-B665-331F51CF4A39");
  }

  public class Upgrade_ExtendObjectAsyncComponent : IGH_UpgradeObject
  {
    public IGH_DocumentObject Upgrade(IGH_DocumentObject target, GH_Document document)
    {
      IGH_Component component = target as IGH_Component;
      if (component == null)
      {
        return null;
      }

      IGH_Component upgradedComponent = GH_UpgradeUtil.SwapComponents(component, UpgradeTo);
      UpgradeUtils.SwapGroups(document, component, upgradedComponent);

      return upgradedComponent;
    }

    public DateTime Version => new DateTime(2021, 6, 20);
    public Guid UpgradeFrom => new Guid("6B1A1705-FDDE-4DE6-9FEA-D31A226F2F66");
    public Guid UpgradeTo => new Guid("2D455B11-F372-47E5-98BE-515EA758A669");
  }

  public class Upgrade_ExtendObjectKeyValueAsyncComponent : IGH_UpgradeObject
  {
    public IGH_DocumentObject Upgrade(IGH_DocumentObject target, GH_Document document)
    {
      IGH_Component component = target as IGH_Component;
      if (component == null)
      {
        return null;
      }

      IGH_Component upgradedComponent = GH_UpgradeUtil.SwapComponents(component, UpgradeTo);
      upgradedComponent.Params.Input[0].Access = GH_ParamAccess.item;
      upgradedComponent.Params.Input[2].Access = GH_ParamAccess.tree;
      UpgradeUtils.SwapGroups(document, component, upgradedComponent);

      return upgradedComponent;
    }

    public DateTime Version => new DateTime(2021, 6, 20);
    public Guid UpgradeFrom => new Guid("00287364-F725-466E-9E38-FDAD270D87D3");
    public Guid UpgradeTo => new Guid("0D862057-254F-40C2-AC4A-9D163BB1E24B");
  }

  public class Upgrade_ObjectValueByKeyAsyncComponent : IGH_UpgradeObject
  {
    public IGH_DocumentObject Upgrade(IGH_DocumentObject target, GH_Document document)
    {
      IGH_Component component = target as IGH_Component;
      if (component == null)
      {
        return null;
      }

      IGH_Component upgradedComponent = GH_UpgradeUtil.SwapComponents(component, UpgradeTo);
      UpgradeUtils.SwapGroups(document, component, upgradedComponent);

      return upgradedComponent;
    }

    public DateTime Version => new DateTime(2021, 6, 20);
    public Guid UpgradeFrom => new Guid("050B24D3-CCEA-466A-B52C-25CB4DA39981");
    public Guid UpgradeTo => new Guid("BA787569-36E6-4522-AC76-B09983E0A40D");
  }
}
