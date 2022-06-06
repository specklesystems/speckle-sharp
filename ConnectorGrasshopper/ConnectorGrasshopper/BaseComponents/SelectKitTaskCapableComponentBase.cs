using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using ConnectorGrasshopper.Extras;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using GrasshopperAsyncComponent;
using Sentry;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;

namespace ConnectorGrasshopper.Objects
{
  public class SelectKitTaskCapableComponentBase<T> : GH_SpeckleTaskCapableComponent<T>
  {
    public ISpeckleConverter Converter;

    public ISpeckleKit Kit;

    public virtual bool CanDisableConversion => true;

    public string SelectedKitName;

    public SelectKitTaskCapableComponentBase(string name, string nickname, string description, string category,
      string subCategory) : base(name, nickname, description, category, subCategory)
    {
    }

    public override void AddedToDocument(GH_Document document)
    {
      base.AddedToDocument(document);
      if (SelectedKitName == null)
      {
        SelectedKitName = SpeckleGHSettings.SelectedKitName;
      }
      SetConverter();
    }

    public virtual bool SetConverter()
    {
      if (SelectedKitName == "None")
      {
        Kit = null;
        Converter = null;
        Message = "No Conversion";
        return true;
      }
      try
      {
        SetConverterFromKit(SelectedKitName);
        return true;
      }
      catch
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No kit found on this machine.");
        return false;
      }
    }

    public override bool Write(GH_IWriter writer)
    {
      writer.SetString("selectedKitName", SelectedKitName);
      return base.Write(writer);
    }

    public override bool Read(GH_IReader reader)
    {
      reader.TryGetString("selectedKitName", ref SelectedKitName);
      return base.Read(reader);
    }

    public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
    {
      //base.AppendAdditionalMenuItems(menu);
      try
      {
        var kits = KitManager.GetKitsWithConvertersForApp(Extras.Utilities.GetVersionedAppName());

        Menu_AppendSeparator(menu);
        Menu_AppendItem(menu, "Select the converter you want to use:", null, false);
        if (CanDisableConversion)
          Menu_AppendItem(menu, "Do Not Convert", (s, e) =>
          {
            SelectedKitName = "None";
            SetConverter();
            ExpireSolution(true);
          }, true, Kit == null);

        foreach (var kit in kits)
        {
          Menu_AppendItem(menu, $"{kit.Name} ({kit.Description})", (s, e) =>
          {
            SelectedKitName = kit.Name; SetConverter(); ExpireSolution(true);
          }, true,
            kit.Name == Kit?.Name);
        }

        Menu_AppendSeparator(menu);
      }
      catch (Exception e)
      {
        Menu_AppendItem(menu, "An error occurred while fetching Kits", null, false);
      }
    }

    public virtual void SetConverterFromKit(string kitName)
    {
      if (kitName == Kit?.Name) return;
      Kit = KitManager.Kits.FirstOrDefault(k => k.Name == kitName);
      SelectedKitName = Kit.Name;
      Converter = Kit.LoadConverter(Extras.Utilities.GetVersionedAppName());
      Converter.SetConverterSettings(SpeckleGHSettings.MeshSettings);
      SpeckleGHSettings.OnMeshSettingsChanged +=
        (sender, args) => Converter.SetConverterSettings(SpeckleGHSettings.MeshSettings);
      Converter.SetContextDocument(Rhino.RhinoDoc.ActiveDoc);
      Message = $"Using the {Kit.Name} Converter";
    }

    public override Guid ComponentGuid => new Guid("2FEE5354-0F5E-41D9-ACD3-BF376D29CCDC");

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      throw new SpeckleException("Please inherit from this class, don't use SelectKitComponentBase directly",
        level: SentryLevel.Warning);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      throw new SpeckleException("Please inherit from this class, don't use SelectKitComponentBase directly",
        level: SentryLevel.Warning);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
      throw new NotImplementedException();
    }

    protected override void BeforeSolveInstance()
    {
      Converter?.SetContextDocument(Rhino.RhinoDoc.ActiveDoc);
      base.BeforeSolveInstance();
    }

    public override void ComputeData()
    {
      //Ensure converter document is up to date
      if (Converter == null)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "No converter was provided. Conversions are disabled.");

      }
      base.ComputeData();
    }
  }
}
