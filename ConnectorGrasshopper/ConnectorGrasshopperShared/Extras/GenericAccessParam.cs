using System;
using System.Windows.Forms;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;

namespace ConnectorGrasshopper.Extras;

public class GenericAccessParam : SpeckleStatefulParam
{
  public override Guid ComponentGuid => new("1D7EF320-D158-453D-8B70-964EFD3C9EDC");

  protected override void Menu_AppendCustomMenuItems(ToolStripDropDown menu)
  {
    Menu_AppendAccessToggle(menu);
    Menu_AppendOptionalToggle(menu);
    Menu_AppendDetachToggle(menu);
  }
}

public class GenericAccessParamAttributes : GH_LinkedParamAttributes
{
  public GenericAccessParamAttributes(IGH_Param param, IGH_Attributes parent)
    : base(param, parent) { }

  public bool IsTabPressed { get; set; }

  public override GH_ObjectResponse RespondToMouseDoubleClick(GH_Canvas sender, GH_CanvasMouseEvent e)
  {
    if ((Control.ModifierKeys & Keys.Shift) == Keys.Shift)
    {
      switch (Owner.Kind)
      {
        case GH_ParamKind.input:
          if (Owner.MutableNickName)
          {
            (Owner as SpeckleStatefulParam)?.InheritNickname();
          }

          return GH_ObjectResponse.Handled;
        case GH_ParamKind.output:
          Clipboard.SetText(DocObject.NickName);
          return GH_ObjectResponse.Handled;
        case GH_ParamKind.unknown:
        case GH_ParamKind.floating:
        default:
          return base.RespondToMouseDoubleClick(sender, e);
      }
    }

    return base.RespondToMouseDoubleClick(sender, e);
  }
}
