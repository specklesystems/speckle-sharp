using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ConnectorGrasshopper.Extras;
using Grasshopper.Kernel;
using Speckle.Core.Models;
using Logging = Speckle.Core.Logging;
using Utilities = ConnectorGrasshopper.Extras.Utilities;

namespace ConnectorGrasshopper.Objects
{
  public class GetObjectValueByKeyTaskComponent : SelectKitTaskCapableComponentBase<object>
  {
    public override Guid ComponentGuid => new Guid("BA787569-36E6-4522-AC76-B09983E0A40D");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override Bitmap Icon => Properties.Resources.GetObjectValueByKey;

    public GetObjectValueByKeyTaskComponent() : base("Speckle Object Value by Key", "Object K/V",
      "Gets the value of a specific key in a Speckle object.", ComponentCategories.PRIMARY_RIBBON, ComponentCategories.OBJECTS)
    {
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddParameter(new SpeckleBaseParam("Object", "O", "Object to get values from.", GH_ParamAccess.item));
      pManager.AddGenericParameter("Key", "K", "List of keys", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddGenericParameter("Value", "V", "Speckle object", GH_ParamAccess.list);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
      if (InPreSolve)
      {
        DA.DisableGapLogic();
        var speckleObj = new GH_SpeckleBase();
        var key = "";
        DA.GetData(0, ref speckleObj);
        DA.GetData(1, ref key);
        if (DA.Iteration == 0)
        {
          Logging.Analytics.TrackEvent(Logging.Analytics.Events.NodeRun, new Dictionary<string, object>() { { "name", "Object Value By Key" } });

        }


        var @base = speckleObj?.Value;
        var task = Task.Run(() => DoWork(@base, key, CancelToken));
        TaskList.Add(task);
        return;
      }

      if (!GetSolveResults(DA, out object value))
      {
        // No result could be obtained.
        return;
      }

      // Report all conversion errors as warnings
      if (Converter != null)
      {
        foreach (var error in Converter.Report.ConversionErrors)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
            error.Message + ": " + error.InnerException?.Message);
        }
        Converter.Report.ConversionErrors.Clear();
      }

      switch (value)
      {
        case null:
          AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Key not found in object");
          break;
        case IEnumerable list:
          {
            var ghGoos = list.Cast<object>().Select(GH_Convert.ToGoo).ToList();
            DA.SetDataList(0, ghGoos);
            break;
          }
        default:
          Params.Output[0].Access = GH_ParamAccess.item;
          DA.SetData(0, GH_Convert.ToGoo(value));
          break;
      }

    }

    private object DoWork(Base @base, string key, CancellationToken token)
    {
      object value = null;
      try
      {
        if (token.IsCancellationRequested)
        {
          return null;
        }

        var obj = @base[key] ?? @base["@" + key];

        switch (obj)
        {
          case null:
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Key not found in object: " + key);
            break;
          case IList list:
            {
              value = list.Cast<object>().Select(
                item => Converter != null ? Utilities.TryConvertItemToNative(item, Converter) : item).ToList();
              break;
            }
          default:
            value = Converter != null ? Utilities.TryConvertItemToNative(obj, Converter) : obj;
            break;
        }
      }
      catch (Exception e)
      {
        // If we reach this, something happened that we weren't expecting...
        Logging.Log.CaptureException(e);
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Something went terribly wrong... " + e.Message);

      }
      return value;
    }
  }
}
