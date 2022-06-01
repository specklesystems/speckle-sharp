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
