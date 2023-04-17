using System;
using System.Windows.Forms;
using Grasshopper.Kernel;

namespace ConnectorGrasshopper.Extras;

public class SendReceiveDataParam : SpeckleStatefulParam
{
  public SendReceiveDataParam()
  {
    SetAccess(GH_ParamAccess.tree);
  }

  public override Guid ComponentGuid => new("79E53524-0533-4D71-BC3D-3E91A854840C");

  protected override void Menu_AppendCustomMenuItems(ToolStripDropDown menu)
  {
    Menu_AppendDetachToggle(menu);
  }
}
