using System;
using System.Windows.Forms;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;

namespace ConnectorGrasshopper.Extras
{
  public class GenericAccessParam : Param_GenericObject
  {
    public override GH_Exposure Exposure => GH_Exposure.hidden;

    public override Guid ComponentGuid => new Guid("1D7EF320-D158-453D-8B70-964EFD3C9EDC");

    public bool Detachable { get; set; } = true;

    public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
    {
      if (Kind != GH_ParamKind.input) return;
      Menu_AppendSeparator(menu);

      var item0 = Menu_AppendItem(menu, "Item Access", (s, e) => { SetAccess(GH_ParamAccess.item); }, true, Access == GH_ParamAccess.item);
      item0.ToolTipText = "Set this parameter as an Item.";
      var item1 = Menu_AppendItem(menu, "List Access", (s, e) => { SetAccess(GH_ParamAccess.list); }, true, Access == GH_ParamAccess.list);
      item1.ToolTipText = "Set this parameter as a List.";

      Menu_AppendSeparator(menu);

      var item2 = Menu_AppendItem(menu, "Detach", (s, e) => { SetDetach(true); }, true, Detachable == true);
      item2.ToolTipText = "Flag this key as detachable.";
      var item3 = Menu_AppendItem(menu, "Do Not Detach", (s, e) => { SetDetach(false); }, true, Detachable == false);
      item3.ToolTipText = "Flag this key as not detachable.";

      Menu_AppendSeparator(menu);

      base.AppendAdditionalMenuItems(menu);
    }

    private void SetDetach(bool state)
    {
      Detachable = state;
      OnObjectChanged(GH_ObjectEventType.DataMapping);
      OnDisplayExpired(true);
      ExpireSolution(true);
    }

    private void SetAccess(GH_ParamAccess accessType)
    {
      Access = accessType;
      OnObjectChanged(GH_ObjectEventType.DataMapping);
      OnDisplayExpired(true);
      ExpireSolution(true);
    }

    public override bool Write(GH_IWriter writer)
    {
      writer.SetBoolean("detachable", Detachable);
      return base.Write(writer);
    }

    public override bool Read(GH_IReader reader)
    {
      bool _detachable = true;
      reader.TryGetBoolean("detachable", ref _detachable);
      Detachable = _detachable;
      return base.Read(reader);
    }

  }
}
