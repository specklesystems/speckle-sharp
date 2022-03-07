using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GH_IO.Serialization;
using Grasshopper;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.GUI.Canvas.Interaction;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using Grasshopper.Kernel.Parameters;

namespace ConnectorGrasshopper.Extras
{
  public class SendReceiveDataParam : SpeckleStatefulParam
  {
    public override Guid ComponentGuid => new Guid("79E53524-0533-4D71-BC3D-3E91A854840C");

    public SendReceiveDataParam()
    {
      SetAccess(GH_ParamAccess.tree);
    }
    
    protected override void Menu_AppendCustomMenuItems(ToolStripDropDown menu)
    {
      Menu_AppendDetachToggle(menu);
    }
  }

}
