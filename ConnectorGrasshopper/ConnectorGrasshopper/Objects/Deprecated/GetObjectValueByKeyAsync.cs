using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using ConnectorGrasshopper.Extras;
using Grasshopper.Kernel;
using GrasshopperAsyncComponent;
using Speckle.Core.Kits;
using Speckle.Core.Logging;

namespace ConnectorGrasshopper.Objects
{
  public class GetObjectValueByKeyAsync : SelectKitAsyncComponentBase
  {
    public override GH_Exposure Exposure => GH_Exposure.hidden;
    protected override Bitmap Icon => Properties.Resources.GetObjectValueByKey;
    public override bool Obsolete => true;

    public override Guid ComponentGuid => new Guid("050B24D3-CCEA-466A-B52C-25CB4DA39981");

    public GetObjectValueByKeyAsync() : base("Speckle Object Value by Key", "Object K/V",
      "Gets the value of a specific key in a Speckle object.", ComponentCategories.PRIMARY_RIBBON, ComponentCategories.OBJECTS)
    {
    }

    public override void SetConverter()
    {
      base.SetConverter();
      BaseWorker = new GetObjectValueByKeyWorker(this, Converter);
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
      try
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
              item => Converter != null ? Utilities.TryConvertItemToNative(item, Converter) : item).ToList();
            break;
          }
          default:
            value = Converter!= null ? Utilities.TryConvertItemToNative(obj, Converter) : obj;
            break;
        }

        Done();
      }
      catch (Exception e)
      {
        // If we reach this, something happened that we weren't expecting...
        Log.CaptureException(e);
        Parent.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Something went terribly wrong... " + e.Message);
        Parent.Message = "Error";

      }
    }

    public override void SetData(IGH_DataAccess DA)
    {
      // Report all conversion errors as warnings
      if(Converter != null)
        foreach (var error in Converter.Report.ConversionErrors)
        {
          Parent.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, error.Message + ": " + error.InnerException?.Message);
        }
      
      switch (value)
      {
        case null:
          Parent.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Key not found in object: " + key);
          break;
        case List<object> list:
        {
          DA.SetDataList(0, list.Select(GH_Convert.ToGoo).ToList());
          break;
        }
        default:
          DA.SetData(0, GH_Convert.ToGoo(value));
          break;
      }
    }

    public override void GetData(IGH_DataAccess DA, GH_ComponentParamServer Params)
    {
      DA.DisableGapLogic();
      var speckleObj = new GH_SpeckleBase();
      DA.GetData(0, ref speckleObj);
      DA.GetData(1, ref key);

      @base = speckleObj?.Value;
    }
  }
}
