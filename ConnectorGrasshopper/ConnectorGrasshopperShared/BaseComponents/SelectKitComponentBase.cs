using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using ConnectorGrasshopper.Extras;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino;
using Sentry;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;

namespace ConnectorGrasshopper.Objects
{
  public class SelectKitComponentBase : GH_SpeckleComponent
  {
    public ISpeckleConverter Converter;

    public ISpeckleKit Kit;
    
    public SelectKitComponentBase(string name, string nickname, string description, string category, string subCategory) : base(name, nickname, description, category, subCategory)
    {
    }

    public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
    {
      try
      {
        var kits = KitManager.GetKitsWithConvertersForApp(Extras.Utilities.GetVersionedAppName());

        Menu_AppendSeparator(menu);
        Menu_AppendItem(menu, "Select the converter you want to use:", null, false);
        foreach (var kit in kits)
        {
          Menu_AppendItem(menu, $"{kit.Name} ({kit.Description})", (s, e) => { SetConverterFromKit(kit.Name); }, true,
            kit.Name == Kit.Name);
        }

        Menu_AppendSeparator(menu);
      }
      catch (Exception e)
      {
        // Todo: handle this
        Console.WriteLine(e);
      }
    }

    public void SetConverterFromKit(string kitName)
    {
      if (kitName == Kit.Name) return;

      try
      {
        Kit = KitManager.Kits.FirstOrDefault(k => k.Name == kitName);
        Converter = Kit.LoadConverter(Extras.Utilities.GetVersionedAppName());
        Converter.SetConverterSettings(SpeckleGHSettings.MeshSettings);
        SpeckleGHSettings.OnMeshSettingsChanged +=
          (sender, args) => Converter.SetConverterSettings(SpeckleGHSettings.MeshSettings);
        Converter.SetContextDocument(Loader.GetCurrentDocument());
        Message = $"Using the {Kit.Name} Converter";
        ExpireSolution(true);
      }
      catch (Exception e)
      {
        // TODO: handle this.
        Console.WriteLine(e);
      }
    }

    public override Guid ComponentGuid => new Guid("18E665F6-29D2-4DCF-96E1-124960AD46A7");

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
      throw new SpeckleException("Please inherit from this class, don't use SelectKitComponentBase directly",
        level: SentryLevel.Warning);
    }

    public override void AddedToDocument(GH_Document document)
    {
      base.AddedToDocument(document);

      try
      {
        Kit = KitManager.GetKitsWithConvertersForApp(Extras.Utilities.GetVersionedAppName())
          .FirstOrDefault(kit => kit.Name == SpeckleGHSettings.SelectedKitName);
        Converter = Kit.LoadConverter(Extras.Utilities.GetVersionedAppName());
        Converter.SetContextDocument(Loader.GetCurrentDocument());
        Converter.SetConverterSettings(SpeckleGHSettings.MeshSettings);
        SpeckleGHSettings.OnMeshSettingsChanged +=
          (sender, args) => Converter.SetConverterSettings(SpeckleGHSettings.MeshSettings);
        Message = $"{Kit.Name} Kit";
      }
      catch
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No default kit found on this machine.");
      }
    }
  }
}
