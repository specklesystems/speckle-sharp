using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ConnectorGrasshopper.Extras;
using ConnectorGrasshopper.Objects;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using GrasshopperAsyncComponent;
using Speckle.Core.Api;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Utilities = ConnectorGrasshopper.Extras.Utilities;

namespace ConnectorGrasshopper.Ops
{
  public class ReceiveLocalComponent : GH_AsyncComponent
  {
    public override GH_Exposure Exposure => GH_Exposure.secondary;

    public ISpeckleConverter Converter;

    public ISpeckleKit Kit;
    public ReceiveLocalComponent() : base("Local Receive", "LR",
      "Receives data locally, without the need of a Speckle Server. NOTE: updates will not be automatically received.",
      ComponentCategories.PRIMARY_RIBBON, ComponentCategories.SEND_RECEIVE)
    {
      BaseWorker = new ReceiveLocalWorker(this);
      SetDefaultKitAndConverter();
    }

    protected override Bitmap Icon => Properties.Resources.LocalReceiver;

    public override Guid ComponentGuid => new Guid("43E22B36-891B-4478-8A4E-2338272EA3B3");

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddGenericParameter("localDataId", "id", "ID of the local data sent.", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddGenericParameter("Data", "D", "Data to send.", GH_ParamAccess.tree);
    }
    
    
    protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
    {
      Menu_AppendSeparator(menu);
      Menu_AppendItem(menu, "Select the converter you want to use:");
      var kits = KitManager.GetKitsWithConvertersForApp(Applications.Rhino);

      foreach (var kit in kits)
      {
        Menu_AppendItem(menu, $"{kit.Name} ({kit.Description})", (s, e) => { SetConverterFromKit(kit.Name); }, true,
          kit.Name == Kit.Name);
      }

      base.AppendAdditionalComponentMenuItems(menu);
    }

    public void SetConverterFromKit(string kitName)
    {
      if (kitName == Kit.Name) return;

      Kit = KitManager.Kits.FirstOrDefault(k => k.Name == kitName);
      Converter = Kit.LoadConverter(Applications.Rhino);

      Message = $"Using the {Kit.Name} Converter";
      ExpireSolution(true);
    }

    private void SetDefaultKitAndConverter()
    {
      Kit = KitManager.GetDefaultKit();
      try
      {
        Converter = Kit.LoadConverter(Applications.Rhino);
        Converter.SetContextDocument(Rhino.RhinoDoc.ActiveDoc);
      }
      catch
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No default kit found on this machine.");
      }
    }

  }
  public class ReceiveLocalWorker : WorkerInstance
  {
    private GH_Structure<IGH_Goo> data;
    private string localDataId;
    public ReceiveLocalWorker(GH_Component _parent) : base(_parent)
    {
    }

    public override WorkerInstance Duplicate() => new ReceiveLocalWorker(Parent);

    public override void DoWork(Action<string, double> ReportProgress, Action Done)
    {
      try
      {
        Parent.Message = "Receiving...";
        var Converter = (Parent as ReceiveLocalComponent).Converter;
        var @base = Operations.Receive(localDataId).Result;
      
        if (Converter.CanConvertToNative(@base))
        {
          var converted = Converter.ConvertToNative(@base);
          data = new GH_Structure<IGH_Goo>();
          data.Append(Utilities.TryConvertItemToNative(converted, Converter));
        }
        else if (@base.GetDynamicMembers().Count() == 1)
        {
          var treeBuilder = new TreeBuilder(Converter);
          var tree = treeBuilder.Build(@base[@base.GetDynamicMembers().ElementAt(0)]);
          data = tree;
        }
        else
        {
          data = new GH_Structure<IGH_Goo>();
          data.Append(new GH_SpeckleBase(@base));
        }
        Done();
      }
      catch (Exception e)
      {
        // If we reach this, something happened that we weren't expecting...
        Log.CaptureException(e);
        Parent.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Something went terribly wrong... " + e.Message);
        Parent.Message = "Error";
        Done();
      }
    }

    public override void SetData(IGH_DataAccess DA)
    {
      DA.SetDataTree(0, data);
      localDataId = null;
    }

    public override void GetData(IGH_DataAccess DA, GH_ComponentParamServer Params)
    {
      DA.GetData(0, ref localDataId);
      data = null;
    }
  }
}
