using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using ConnectorGrasshopper.Extras;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Sentry;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;

namespace ConnectorGrasshopper.Objects
{
  public class SelectKitComponentBase : GH_Component
  {
    public ISpeckleConverter Converter;

    public ISpeckleKit Kit;

    public SelectKitComponentBase(string name, string nickname, string description, string category, string subCategory) : base(name, nickname, description, category, subCategory)
    {
      var key = "Speckle2:kit.default.name";
      var n = Grasshopper.Instances.Settings.GetValue(key, "Objects");
      Kit = KitManager.GetKitsWithConvertersForApp(Applications.Rhino).FirstOrDefault(kit => kit.Name == n);
      try
      {
        Converter = Kit.LoadConverter(Applications.Rhino);
        Converter.SetContextDocument(Rhino.RhinoDoc.ActiveDoc);
        Message = $"{Kit.Name} Kit";
      }
      catch
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No default kit found on this machine.");
      }
    }

    public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
    {
      Menu_AppendSeparator(menu);
      var menuItem = Menu_AppendItem(menu, "Select the converter you want to use:");
      menuItem.Enabled = false;
      var kits = KitManager.GetKitsWithConvertersForApp(Applications.Rhino);

      foreach (var kit in kits)
      {
        Menu_AppendItem(menu, $"{kit.Name} ({kit.Description})", (s, e) => { SetConverterFromKit(kit.Name); }, true, kit.Name == Kit.Name);
      }

      Menu_AppendSeparator(menu);
    }

    public void SetConverterFromKit(string kitName)
    {
      if (kitName == Kit.Name)return;

      Kit = KitManager.Kits.FirstOrDefault(k => k.Name == kitName);
      Converter = Kit.LoadConverter(Applications.Rhino);
      Converter.SetContextDocument(Rhino.RhinoDoc.ActiveDoc);

      Message = $"Using the {Kit.Name} Converter";
      ExpireSolution(true);
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
  }
}
