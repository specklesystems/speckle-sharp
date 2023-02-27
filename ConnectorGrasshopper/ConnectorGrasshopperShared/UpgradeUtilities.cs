using System;
using System.Linq;
using ConnectorGrasshopper.Extras;
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

    public static void SwapParameter(IGH_Component component, int index, IGH_Param target)
    {
      var source = component.Params.Input[index];
      GH_UpgradeUtil.MigrateSources(source, target);
      component.Params.Input.Remove(source);
      component.Params.Input.Insert(index, target);
    }
  }
}
