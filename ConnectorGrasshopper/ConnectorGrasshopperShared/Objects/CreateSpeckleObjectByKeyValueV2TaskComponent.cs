using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using ConnectorGrasshopper.Extras;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Serilog;
using Speckle.Core.Models;
using Speckle.Core.Models.Extensions;
using Logging = Speckle.Core.Logging;
using Utilities = ConnectorGrasshopper.Extras.Utilities;

namespace ConnectorGrasshopper.Objects
{
  public class CreateSpeckleObjectByKeyValueV2TaskComponent : SelectKitTaskCapableComponentBase<Base>
  {
    public CreateSpeckleObjectByKeyValueV2TaskComponent() : base("Create Speckle Object by Key/Value", "CSOKV",
      "Creates a speckle object from key value pairs", ComponentCategories.PRIMARY_RIBBON, ComponentCategories.OBJECTS)
    {
    }

    public override Guid ComponentGuid => new Guid("7BC0B11A-A662-4D95-8A8D-79950BDBF251");

    protected override Bitmap Icon => Properties.Resources.CreateSpeckleObjectByKeyValue;
    public override GH_Exposure Exposure => GH_Exposure.primary | GH_Exposure.obscure;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddTextParameter("Keys", "K", "List of keys", GH_ParamAccess.list);
      pManager.AddGenericParameter("Values", "V", "List of values", GH_ParamAccess.list);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddParameter(new SpeckleBaseParam("Object", "O", "Speckle object", GH_ParamAccess.item));
    }

    public override void SolveInstanceWithLogContext(IGH_DataAccess DA)
    {
      if (InPreSolve)
      {
        var keys = new List<string>();
        var values = new List<IGH_Goo>();
        if (DA.Iteration == 0)
          Tracker.TrackNodeRun("Create Object By Key Value");


        DA.GetDataList(0, keys);
        DA.GetDataList(1, values);
        TaskList.Add(Task.Run(() => DoWork(keys, values)));
      }
      else
      {
        if (Converter != null)
        {
          foreach (var error in Converter.Report.ConversionErrors)
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, error.ToFormattedString());

          Converter.Report.ConversionErrors.Clear();
        }

        if (!GetSolveResults(DA, out var result))
        {
          // Normal mode not supported
          return;
        }

        if (result != null) DA.SetData(0, result);
      }
    }


    public Base DoWork(List<string> keys, List<IGH_Goo> values)
    {
      try
      {
        // 👉 Checking for cancellation!
        if (CancelToken.IsCancellationRequested) return null;

        if (keys.Count != values.Count)
          throw new Exception("Keys and Values list do not have the same number of items");

        var speckleObj = new Base();
        for (var i = 0; i < keys.Count; i++)
        {
          var key = keys[i];
          var value = values[i];
          try
          {
            if (value is SpeckleObjectGroup group)
              speckleObj[key] = Converter != null ? group.Value.Select(item => Utilities.TryConvertItemToSpeckle(item, Converter)).ToList() : group.Value;
            else
              speckleObj[key] = Converter != null ? Utilities.TryConvertItemToSpeckle(value, Converter) : value;
          }
          catch (Exception e)
          {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, e.ToFormattedString());
          }
        }

        if (speckleObj.GetMembers(DynamicBaseMemberType.Dynamic).Count == 0)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Resulting object had no properties. Output will be null");
          return null;
        }

        return speckleObj;
      }
      catch (Exception ex)
      {
        // If we reach this, something happened that we weren't expecting...
        Logging.SpeckleLog.Logger.Error(ex, ex.Message);
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Could not create Speckle object: " + ex.ToFormattedString());
        return null;
      }
    }
  }
}
