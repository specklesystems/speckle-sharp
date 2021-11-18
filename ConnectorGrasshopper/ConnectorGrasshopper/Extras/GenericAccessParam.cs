using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using GH_IO.Serialization;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;


namespace ConnectorGrasshopper.Extras
{
  public class GenericAccessParam : Param_GenericObject
  {
    public override GH_Exposure Exposure => GH_Exposure.hidden;

    public override Guid ComponentGuid => new Guid("1D7EF320-D158-453D-8B70-964EFD3C9EDC");

    public override GH_StateTagList StateTags
    {
      get
      {
        var tags = base.StateTags;
        if (Kind != GH_ParamKind.input) return tags;
        if (Optional)
        {
          tags.Add(new OptionalStateTag());
        }

        if (Detachable)
          tags.Add(new DetachedStateTag());
        if (Access == GH_ParamAccess.list)
          tags.Add(new ListAccesStateTag());

        return tags;
      }
    }

    public bool Detachable { get; set; } = true;

    public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
    {
      if (Kind != GH_ParamKind.input)
      {
        // Append graft,flatten,etc... options to outputs.
        base.AppendAdditionalMenuItems(menu);
        return;
      }

      Menu_AppendSeparator(menu);

      var listAccessToggle = Menu_AppendItem(
        menu,
        "List Access",
        (s, e) => SetAccess(Access == GH_ParamAccess.list ? GH_ParamAccess.item : GH_ParamAccess.list),
        true,
        Access == GH_ParamAccess.list);
      listAccessToggle.ToolTipText = "Set this parameter as a List. If disabled, defaults to item access.";
      listAccessToggle.Image = Properties.Resources.StateTag_List;

      var optionalToggle = Menu_AppendItem(
        menu,
        "Optional",
        (sender, args) => SetOptional(!Optional),
        Properties.Resources.speckle_logo,
        true,
        Optional);
      optionalToggle.ToolTipText = "Set this parameter as optional.";
      optionalToggle.Image = Properties.Resources.StateTag_Optional;

      var detachToggle = Menu_AppendItem(
        menu,
        "Detach property",
        (s, e) => SetDetach(!Detachable),
        true,
        Detachable);
      detachToggle.ToolTipText = "Sets this key as detachable.";
      detachToggle.Image = Properties.Resources.StateTag_Detach;

      Menu_AppendSeparator(menu);

      base.AppendAdditionalMenuItems(menu);
    }

    protected override void ValuesChanged()
    {
      base.ValuesChanged();
    }

    protected override void OnVolatileDataCollected()
    {
      base.OnVolatileDataCollected();
    }

    private void HandleParamStateChange()
    {
      OnObjectChanged(GH_ObjectEventType.DataMapping);
      OnDisplayExpired(true);
      ExpireSolution(true);
    }

    private void SetOptional(bool state)
    {
      Optional = state;
      HandleParamStateChange();
    }

    private void SetDetach(bool state)
    {
      Detachable = state;
      HandleParamStateChange();
    }

    private void SetAccess(GH_ParamAccess accessType)
    {
      Access = accessType;
      HandleParamStateChange();
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
