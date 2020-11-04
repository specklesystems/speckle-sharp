using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ConnectorGrasshopper.Extras;
using Grasshopper.Kernel;
using GrasshopperAsyncComponent;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Utilities = ConnectorGrasshopper.Extras.Utilities;

namespace ConnectorGrasshopper.Objects
{
  public class CreateSpeckleObjectAsync : GH_AsyncComponent, IGH_VariableParameterComponent
  {
    private System.Timers.Timer Debouncer;

    public ISpeckleConverter Converter;

    public ISpeckleKit Kit;

    protected override Bitmap Icon => Properties.Resources.CreateSpeckleObject;

    public override GH_Exposure Exposure => GH_Exposure.hidden;

    public override Guid ComponentGuid => new Guid("FC2EF86F-2C12-4DC2-B216-33BFA409A0FC");


    public CreateSpeckleObjectAsync() : base("Create Speckle Object Async", "CSOA",
      "Allows you to create a Speckle object by setting its keys and values.",
      "Speckle 2 Dev", "Async Object Management")
    {
      Kit = KitManager.GetDefaultKit();
      try
      {
        Converter = Kit.LoadConverter(Applications.Rhino);
        BaseWorker = new CreateSpeckleObjectWorker(this,Converter);
        Message = $"{Kit.Name} Kit";
      }
      catch
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No default kit found on this machine.");
      }
    }

    public override void AddedToDocument(GH_Document document)
    {
      base.AddedToDocument(document);
      Debouncer = new System.Timers.Timer(2000) {AutoReset = false};
      Debouncer.Elapsed += (s, e) => Rhino.RhinoApp.InvokeOnUiThread((Action) delegate { this.ExpireSolution(true); });

      foreach (var param in Params.Input)
        param.ObjectChanged += (s, e) => Debouncer.Start();
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddParameter(new SpeckleBaseParam("Speckle Object", "O", "Created speckle object", GH_ParamAccess.item));
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
      BaseWorker = new CreateSpeckleObjectWorker(this, Converter);
      Message = $"Using the {Kit.Name} Converter";
      ExpireSolution(true);
    }

    public bool CanInsertParameter(GH_ParameterSide side, int index) => side == GH_ParameterSide.Input;

    public bool CanRemoveParameter(GH_ParameterSide side, int index) => side == GH_ParameterSide.Input;

    public IGH_Param CreateParameter(GH_ParameterSide side, int index)
    {
      var myParam = new GenericAccessParam
      {
        Name = GH_ComponentParamServer.InventUniqueNickname("ABCD", Params.Input),
        MutableNickName = true,
        Optional = true
      };

      myParam.NickName = myParam.Name;
      myParam.ObjectChanged += (sender, e) => Debouncer.Start();

      return myParam;
    }

    public bool DestroyParameter(GH_ParameterSide side, int index)
    {
      return true;
    }

    public void VariableParameterMaintenance()
    {
    }
  }

  public class CreateSpeckleObjectWorker : WorkerInstance
  {
    public Base @base;
    public ISpeckleConverter Converter;
    private Dictionary<string, object> inputData;
    public CreateSpeckleObjectWorker(GH_Component parent, ISpeckleConverter converter) : base(parent)
    {
      Converter = converter;
      inputData = new Dictionary<string, object>();
    }

    public override WorkerInstance Duplicate() => new CreateSpeckleObjectWorker(Parent, Converter);

    public override void DoWork(Action<string, double> ReportProgress, Action Done)
    {
      @base = new Base();
      inputData.Keys.ToList().ForEach(key =>
      {
        var value = inputData[key];
        if (value == null)
        {
        }
        else if (value is List<object> list)
        {
          // Value is a list of items, iterate and convert.
          var converted = list.Select(item => Utilities.TryConvertItemToSpeckle(item, Converter)).ToList();
          @base[key] = converted;
        }
        else
        {
          // If value is not list, it is a single item.
          var obj = Utilities.TryConvertItemToSpeckle(value, Converter);
          @base[key] = obj;
        }
      });

      Done();
    }

    public override void SetData(IGH_DataAccess DA)
    {
      DA.SetData(0, new GH_SpeckleBase{ Value = @base });
    }

    public override void GetData(IGH_DataAccess DA, GH_ComponentParamServer Params)
    {
      Params.Input.ForEach(ighParam =>
      {
        var param = ighParam as GenericAccessParam;
        var index = Params.IndexOfInputParam(param.Name);
        var detachable = param.Detachable;
        var key = detachable ? "@" + param.NickName : param.NickName;

        switch (param.Access)
        {
          case GH_ParamAccess.item:
            object value = null;
            DA.GetData(index, ref value);
            inputData[key] = value;
            break;
          case GH_ParamAccess.list:
            var values = new List<object>();
            DA.GetDataList(index, values);
            inputData[key] = values;
            break;
          case GH_ParamAccess.tree:
            break;
          default:
            throw new ArgumentOutOfRangeException();
        }
      });
    }
  }
}
