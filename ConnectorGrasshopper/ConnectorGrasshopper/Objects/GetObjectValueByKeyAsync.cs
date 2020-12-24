using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ConnectorGrasshopper.Extras;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using GrasshopperAsyncComponent;
using Speckle.Core.Kits;

namespace ConnectorGrasshopper.Objects
{
  public class GetObjectValueByKeyAsync : GH_AsyncComponent
  {
    public ISpeckleConverter Converter;
    public ISpeckleKit Kit;

    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override Bitmap Icon => Properties.Resources.GetObjectValueByKey;

    public override Guid ComponentGuid => new Guid("050B24D3-CCEA-466A-B52C-25CB4DA39981");

    public GetObjectValueByKeyAsync() : base("Speckle Object Value by Key Async", "Object K/V A",
      "Gets the value of a specific key in a Speckle object.", "Speckle 2 Dev", "Async Object Management")
    {
      Kit = KitManager.GetDefaultKit();
      try
      {
        Converter = Kit.LoadConverter(Applications.Rhino);
        BaseWorker = new GetObjectValueByKeyWorker(this, Converter);
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
        Menu_AppendItem(menu, $"{kit.Name} ({kit.Description})", (s, e) => { SetConverterFromKit(kit.Name); }, true,
          kit.Name == Kit.Name);
      }

      Menu_AppendSeparator(menu);
    }

    public void SetConverterFromKit(string kitName)
    {
      if (kitName == Kit.Name) return;

      Kit = KitManager.Kits.FirstOrDefault(k => k.Name == kitName);
      Converter = Kit.LoadConverter(Applications.Rhino);
      BaseWorker = new ExpandSpeckleObjectWorker(this, Converter);
      Message = $"Using the {Kit.Name} Converter";
      ExpireSolution(true);
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddParameter(new SpeckleBaseParam("Object", "O", "Object to get values from.", GH_ParamAccess.item));
      pManager.AddGenericParameter("Key", "K", "List of keys", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddGenericParameter("Value", "V", "Speckle object", GH_ParamAccess.item);
    }
  }

  public class GetObjectValueByKeyWorker : WorkerInstance
  {
    public ISpeckleConverter Converter;
    private Speckle.Core.Models.Base @base;
    private string key;
    private object value;

    public GetObjectValueByKeyWorker(GH_Component _parent, ISpeckleConverter converter) : base(_parent)
    {
      Converter = converter;
    }

    public override WorkerInstance Duplicate() => new GetObjectValueByKeyWorker(Parent, Converter);

    public override void DoWork(Action<string, double> ReportProgress, Action Done)
    {
      Parent.Message = "Working";
      
      if (CancellationToken.IsCancellationRequested)
      {
        Done();
        Parent.Message = "Cancelled";
        return;
      }
      
      var obj = @base[key] ?? @base["@" + key];
      switch (obj)
      {
        case null:
          Parent.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Key not found in object: " + key);
          break;
        case List<object> list:
        {
          value = list.Select(
            item => Utilities.TryConvertItemToNative(item, Converter)).ToList();
          break;
        }
        default:
          value = Utilities.TryConvertItemToNative(obj, Converter);
          break;
      }

      Done();
    }

    public override void SetData(IGH_DataAccess DA)
    {
      switch (value)
      {
        case null:
          Parent.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Key not found in object: " + key);
          break;
        case List<object> list:
        {
          DA.SetDataList(0, list.Select(item => new GH_ObjectWrapper(item)).ToList());
          break;
        }
        default:
          DA.SetData(0, new GH_ObjectWrapper(value));
          break;
      }
    }

    public override void GetData(IGH_DataAccess DA, GH_ComponentParamServer Params)
    {
      var speckleObj = new GH_SpeckleBase();
      DA.GetData(0, ref speckleObj);
      DA.GetData(1, ref key);

      @base = speckleObj?.Value;
    }
  }
}
