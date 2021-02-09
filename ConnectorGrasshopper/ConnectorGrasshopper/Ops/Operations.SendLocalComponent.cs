using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Speckle.Core.Models;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.Kernel.Data;
using GrasshopperAsyncComponent;
using Speckle.Core.Api;
using Speckle.Core.Kits;
using Utilities = ConnectorGrasshopper.Extras.Utilities;

namespace ConnectorGrasshopper.Ops
{
  public class SendLocalComponent : GH_AsyncComponent
  {
    public override GH_Exposure Exposure => GH_Exposure.secondary;

    public ISpeckleConverter Converter;

    public ISpeckleKit Kit;
    public SendLocalComponent() : base("Local sender", "LS", "Sends data locally, without the need of a Speckle Server.", ComponentCategories.PRIMARY_RIBBON, ComponentCategories.SEND_RECEIVE)
    {
      BaseWorker = new SendLocalWorker(this);
      SetDefaultKitAndConverter();
    }

    protected override Bitmap Icon => Properties.Resources.LocalSender;

    public override Guid ComponentGuid => new Guid("80AC1649-FF36-4B8B-A5B4-320E9D88F8BF");

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddGenericParameter("Data", "D", "Data to send.", GH_ParamAccess.tree);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddGenericParameter("localDataId", "id", "ID of the local data sent.", GH_ParamAccess.item);
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
  
  public class SendLocalWorker : WorkerInstance
  {
    private GH_Structure<IGH_Goo> data;
    private string sentObjectId;
    public SendLocalWorker(GH_Component _parent) : base(_parent)
    {
    }

    public override WorkerInstance Duplicate() => new SendLocalWorker(Parent);

    public override void DoWork(Action<string, double> ReportProgress, Action Done)
    {
      Parent.Message = "Sending...";
      var converter = (Parent as SendLocalComponent)?.Converter;
      converter?.SetContextDocument(Rhino.RhinoDoc.ActiveDoc);
      var converted = Utilities.DataTreeToNestedLists(data, converter);
      var ObjectToSend = new Base();
      ObjectToSend["@data"] = converted;
      sentObjectId = Operations.Send(ObjectToSend).Result;
      Done();
    }

    public override void SetData(IGH_DataAccess DA)
    {
      DA.SetData(0, sentObjectId);
      data = null;
    }

    public override void GetData(IGH_DataAccess DA, GH_ComponentParamServer Params)
    {
      DA.GetDataTree(0, out data);
      sentObjectId = null;
    }
  }
}
