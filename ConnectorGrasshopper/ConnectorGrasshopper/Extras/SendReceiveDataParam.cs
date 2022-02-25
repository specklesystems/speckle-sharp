using System;
using System.Windows.Forms;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;

namespace ConnectorGrasshopper.Extras
{
  public class SendReceiveDataParam : Param_GenericObject
  {
    public override GH_Exposure Exposure => GH_Exposure.hidden;

    public override Guid ComponentGuid => new Guid("79E53524-0533-4D71-BC3D-3E91A854840C");

    public SendReceiveDataParam()
    {
      
      SetAccess(GH_ParamAccess.tree);
    }

    public override GH_StateTagList StateTags
    {
      get
      {
        var tags = base.StateTags;
        if (Detachable)
          tags.Add(new DetachedStateTag());
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

    public void SetDetach(bool state)
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
