using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ConnectorGrasshopper.Extras;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using GrasshopperAsyncComponent;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Utilities = ConnectorGrasshopper.Extras.Utilities;

namespace ConnectorGrasshopper.Objects
{
  public class ExtendSpeckleObjectAsync : SelectKitAsyncComponentBase
  {
    public override Guid ComponentGuid => new Guid("00287364-F725-466E-9E38-FDAD270D87D3");
    protected override Bitmap Icon => Properties.Resources.ExtendSpeckleObject;

    public override GH_Exposure Exposure => GH_Exposure.secondary;

    public ExtendSpeckleObjectAsync() : base("Extend Speckle Object", "ESO",
      "Extend a current object with key/value pairs", ComponentCategories.PRIMARY_RIBBON, ComponentCategories.OBJECTS)
    {
      BaseWorker = new ExtendSpeckleObjectWorker(this, Converter);
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddParameter(new SpeckleBaseParam("Speckle Object", "O", "Speckle object to extend.", GH_ParamAccess.item));
      pManager.AddTextParameter("Keys", "K", "List of keys", GH_ParamAccess.list);
      pManager.AddGenericParameter("Values", "V", "List of values", GH_ParamAccess.list);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddParameter(new SpeckleBaseParam("Speckle Object", "O",
        "Extended Speckle object.", GH_ParamAccess.item));
    }
  }

  public class ExtendSpeckleObjectWorker : WorkerInstance
  {
    private Base @base;
    private List<string> keys;
    private List<object> values;
    public ISpeckleConverter Converter;

    public ExtendSpeckleObjectWorker(GH_Component _parent, ISpeckleConverter converter) : base(_parent)
    {
      Converter = converter;
      keys = new List<string>();
      values = new List<object>();
    }

    public override WorkerInstance Duplicate()
    {
      return new ExtendSpeckleObjectWorker(Parent, Converter);
    }

    private bool AssignToObject(Base b, List<string> keys, List<IGH_Goo> values)
    {
      var index = 0;
      var hasErrors = false;
      keys.ForEach(key =>
      {
        if (b[key] != null)
        {
          RuntimeMessages.Add((GH_RuntimeMessageLevel.Remark, $"Object {b.id} - Property {key} has been overwritten"));
        }

        try
        {
          b[key] = Utilities.TryConvertItemToSpeckle(values[index++], Converter);
        }
        catch (Exception e)
        {
          RuntimeMessages.Add((GH_RuntimeMessageLevel.Error, e.Message));
          hasErrors = true;
        }
      });

      return hasErrors;
    }

    public override void DoWork(Action<string, double> ReportProgress, Action Done)
    {
      // TODO
      if(keys.Count > values.Count)
      {
        RuntimeMessages.Add((GH_RuntimeMessageLevel.Remark, "more keys than values"));
      }

      if(keys.Count < values.Count)
      {
        RuntimeMessages.Add((GH_RuntimeMessageLevel.Remark, "more values than keys"));
      }

      for(int i = 0; i < keys.Count; i++)
      {
        try
        {
          var value = i < values.Count ? values[i] : values[values.Count - 1];
          @base[keys[i]] = Utilities.TryConvertItemToSpeckle(value, Converter);
        } catch(Exception e)
        {
          RuntimeMessages.Add((GH_RuntimeMessageLevel.Warning, e.Message));
        }
      }

      Done();
    }

    List<(GH_RuntimeMessageLevel, string)> RuntimeMessages { get; set; } = new List<(GH_RuntimeMessageLevel, string)>();

    public override void SetData(IGH_DataAccess DA)
    {
      // 👉 Checking for cancellation!
      if (CancellationToken.IsCancellationRequested) return;

      foreach (var (level, message) in RuntimeMessages)
      {
        Parent.AddRuntimeMessage(level, message);
      }
      
      DA.SetData(0, new GH_SpeckleBase { Value = @base });
    }

    public override void GetData(IGH_DataAccess DA, GH_ComponentParamServer Params)
    {
      DA.DisableGapLogic();
      GH_SpeckleBase ghBase = null;
      DA.GetData(0, ref ghBase);
      DA.GetDataList(1, keys);
      DA.GetDataList(2, values);

      if (ghBase == null || keys.Count == 0 || values.Count == 0)
      {
        return;
      }

      @base = ghBase.Value.ShallowCopy();
    }
  }
}
