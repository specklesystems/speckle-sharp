using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using GH_IO;
using GH_IO.Serialization;
using Grasshopper;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
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

        if (Kind == GH_ParamKind.input)
        {
          if (Optional)
            tags.Add(new OptionalStateTag());
          if (Detachable)
            tags.Add(new DetachedStateTag());
          if (Access == GH_ParamAccess.list)
            tags.Add(new ListAccesStateTag());
        }
        else if (Kind == GH_ParamKind.output)
        {
          if (Detachable)
            tags.Add(new DetachedStateTag());
        }

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
        if(Kind == GH_ParamKind.output)
          Menu_AppendExtractParameter(menu);
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


    protected new void Menu_AppendExtractParameter(ToolStripDropDown menu) => Menu_AppendItem(menu, "Extract parameter", Menu_ExtractParameterClicked, Recipients.Count == 0);

    private void Menu_ExtractParameterClicked(object sender, EventArgs e)
    {
      var ghArchive = new GH_Archive();
      if (!ghArchive.AppendObject(this, "Parameter"))
      {
        Tracing.Assert(new Guid("{96ACE3FC-F716-4b2e-B226-9E2D1F9DA229}"), "Parameter serialization failed.");
      }
      else
      {
        var ghDocumentObject = Instances.ComponentServer.EmitObject(this.ComponentGuid);
        if (ghDocumentObject == null)
          return;
        var ghParam = (IGH_Param) ghDocumentObject;
        ghParam.CreateAttributes();
        if (!ghArchive.ExtractObject(ghParam, "Parameter"))
        {
          Tracing.Assert(new Guid("{2EA6E057-E390-4fc5-B9AB-1B74A8A17625}"), "Parameter deserialization failed.");
        }
        else
        {
          ghParam.NewInstanceGuid();
          ghParam.Attributes.Selected = false;
          ghParam.Attributes.Pivot = new PointF(this.Attributes.Pivot.X + 120f, this.Attributes.Pivot.Y);
          ghParam.Attributes.ExpireLayout();
          ghParam.MutableNickName = true;
          if (ghParam.Attributes is GH_FloatingParamAttributes)
            ((GH_Attributes<IGH_Param>) ghParam.Attributes).PerformLayout();
          var ghDocument = OnPingDocument();
          if (ghDocument == null)
          {
            Tracing.Assert(new Guid("{D74F80C4-CA72-4dbd-8597-450D27098F55}"), "Document could not be located.");
          }
          else
          {
            ghDocument.AddObject(ghParam, false);
            ghParam.AddSource(this);
            ghParam.ExpireSolution(true);
          }
        }
      }
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
