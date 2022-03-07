using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GH_IO.Serialization;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using Grasshopper.Kernel.Parameters;

namespace ConnectorGrasshopper.Extras
{
  public abstract class SpeckleStatefulParam : Param_GenericObject
  {
    public override GH_Exposure Exposure => GH_Exposure.hidden;
    public bool Detachable { get; set; } = true;

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
    
    protected void SetOptional(bool state)
    {
      Optional = state;
      HandleParamStateChange();
    }

    protected void SetDetach(bool state)
    {
      Detachable = state;
      HandleParamStateChange();
    }

    protected void SetAccess(GH_ParamAccess accessType)
    {
      Access = accessType;
      HandleParamStateChange();
    }
    
    private void HandleParamStateChange()
    {
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

    public override void AddSource(IGH_Param source, int index)
    {
      base.AddSource(source, index);
      if (KeyWatcher.TabPressed)
        InheritNickname();
      
    }

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

      Menu_AppendInheritNickname(menu);
      
      Menu_AppendSeparator(menu);

      Menu_AppendCustomMenuItems(menu);

      Menu_AppendSeparator(menu);

      base.AppendAdditionalMenuItems(menu);
    }

    protected abstract void Menu_AppendCustomMenuItems(ToolStripDropDown menu);

    public void InheritNickname()
    {
      RecordUndoEvent("Input name change");
      var names = Sources.Select(s => s.NickName).ToList();
      var fullname = string.Join("|", names).Trim();

      if (string.IsNullOrEmpty(fullname))
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Could not inherit name from parameter '{NickName}': Inherited nickname was empty.");
        ExpirePreview(true);
        return;
      }
      
      Name = fullname;
      NickName = fullname;
      if (Kind == GH_ParamKind.input || Kind == GH_ParamKind.output)
      {
        // If it belongs to a component, expire the component solution
        // This will force a CSO component to update the object with it's new prop name.
        Attributes.Parent.DocObject.ExpireSolution(true);
      }
      else
      {
        // It could also be a standalone component, in which case just expire the preview.
        ExpirePreview(true);
      }
    }
    
    protected new void Menu_AppendExtractParameter(ToolStripDropDown menu) => Menu_AppendItem(menu, "Extract parameter", Menu_ExtractOutputParameterClicked, Recipients.Count == 0);
    
    protected void Menu_AppendAccessToggle(ToolStripDropDown menu)
    {
      var listAccessToggle = Menu_AppendItem(
        menu,
        "List Access",
        (s, e) => SetAccess(Access == GH_ParamAccess.list ? GH_ParamAccess.item : GH_ParamAccess.list),
        true,
        Access == GH_ParamAccess.list);
      listAccessToggle.ToolTipText = "Set this parameter as a List. If disabled, defaults to item access.";
      listAccessToggle.Image = Properties.Resources.StateTag_List;
    }

    protected void Menu_AppendDetachToggle(ToolStripDropDown menu)
    {
      var detachToggle = Menu_AppendItem(
        menu,
        "Detach property",
        (s, e) => SetDetach(!Detachable),
        true,
        Detachable);
      detachToggle.ToolTipText = "Sets this key as detachable.";
      detachToggle.Image = Properties.Resources.StateTag_Detach;
    }

    protected void Menu_AppendOptionalToggle(ToolStripDropDown menu)
    {
      var optionalToggle = Menu_AppendItem(
        menu,
        "Optional",
        (sender, args) => SetOptional(!Optional),
        Properties.Resources.speckle_logo,
        true,
        Optional);
      optionalToggle.ToolTipText = "Set this parameter as optional.";
      optionalToggle.Image = Properties.Resources.StateTag_Optional;
    }
    
    protected void Menu_AppendInheritNickname(ToolStripDropDown menu)
    {
      Menu_AppendItem(
        menu,
        "Inherit names",
        (sender, args) => { InheritNickname(); });
    }
    
    // Decompiled from Grasshopper implementation and modified for output recipient.
    // If you don't know what this is, don't touch it 👍🏼
    protected void Menu_ExtractOutputParameterClicked(object sender, EventArgs e)
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
          ghParam.Attributes.Pivot = new PointF(this.Attributes.Pivot.X + 40f, this.Attributes.Pivot.Y);
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

  }
}
