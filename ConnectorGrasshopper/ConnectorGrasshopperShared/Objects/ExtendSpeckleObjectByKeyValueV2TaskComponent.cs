using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using ConnectorGrasshopper.Extras;
using ConnectorGrasshopper.Properties;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Models.Extensions;
using Utilities = ConnectorGrasshopper.Extras.Utilities;

namespace ConnectorGrasshopper.Objects;

public class ExtendSpeckleObjectByKeyValueV2TaskComponent : SelectKitTaskCapableComponentBase<Base>
{
  public ExtendSpeckleObjectByKeyValueV2TaskComponent()
    : base(
      "Extend Speckle Object by Key/Value",
      "ESOKV",
      "Extend a current object with key/value pairs",
      ComponentCategories.PRIMARY_RIBBON,
      ComponentCategories.OBJECTS
    ) { }

  public override Guid ComponentGuid => new("A72EE68B-218D-41D5-8E23-61369A5A5B55");
  protected override Bitmap Icon => Resources.ExtendSpeckleObjectByKeyValue;
  public override GH_Exposure Exposure => GH_Exposure.secondary | GH_Exposure.obscure;

  protected override void RegisterInputParams(GH_InputParamManager pManager)
  {
    pManager.AddGenericParameter(
      "Speckle Object",
      "O",
      "Speckle object to extend. If the input is not a Speckle Object, it will attempt a conversion of the input first.",
      GH_ParamAccess.item
    );
    pManager.AddTextParameter("Keys", "K", "List of keys", GH_ParamAccess.list);
    pManager.AddGenericParameter("Values", "V", "List of values", GH_ParamAccess.list);
  }

  protected override void RegisterOutputParams(GH_OutputParamManager pManager)
  {
    pManager.AddParameter(
      new SpeckleBaseParam(
        "Extended Speckle Object",
        "EO",
        "The resulting extended Speckle object.",
        GH_ParamAccess.item
      )
    );
  }

  public override void SolveInstanceWithLogContext(IGH_DataAccess DA)
  {
    if (InPreSolve)
    {
      IGH_Goo inputObj = null;
      var keys = new List<string>();
      var valueTree = new List<IGH_Goo>();

      DA.GetData(0, ref inputObj);
      DA.GetDataList(1, keys);
      DA.GetDataList(2, valueTree);

      if (DA.Iteration == 0)
        Tracker.TrackNodeRun("Extend Object By Key Value");

      TaskList.Add(Task.Run(() => DoWork(inputObj, keys, valueTree)));
      return;
    }

    if (Converter != null)
    {
      foreach (var error in Converter.Report.ConversionErrors)
        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, error.ToFormattedString());

      Converter.Report.ConversionErrors.Clear();
    }

    if (!GetSolveResults(DA, out var result))
      // Normal mode not supported
      return;

    if (result != null)
      DA.SetData(0, result);
  }

  public Base DoWork(object inputObj, List<string> keys, List<IGH_Goo> values)
  {
    try
    {
      Base @base;
      if (inputObj is GH_SpeckleBase speckleBase)
      {
        @base = speckleBase.Value.ShallowCopy();
      }
      else
      {
        if (inputObj != null)
        {
          var value = inputObj.GetType().GetProperty("Value")?.GetValue(inputObj);
          if (Converter.CanConvertToSpeckle(value))
          {
            @base = Converter.ConvertToSpeckle(value);
            AddRuntimeMessage(
              GH_RuntimeMessageLevel.Remark,
              "Input object was not a Speckle object, but has been converted to one."
            );
          }
          else
          {
            AddRuntimeMessage(
              GH_RuntimeMessageLevel.Error,
              "Input object is not a Speckle object, nor can it be converted to one."
            );
            return null;
          }
        }
        else
        {
          AddRuntimeMessage(
            GH_RuntimeMessageLevel.Error,
            "Input object is not a Speckle object, nor can it be converted to one."
          );
          return null;
        }
      }

      // 👉 Checking for cancellation!
      if (CancelToken.IsCancellationRequested)
        return null;

      if (keys.Count != values.Count)
        throw new Exception("Keys and Values list do not have the same number of items");

      // We got a list of values

      var hasErrors = false;
      for (var i = 0; i < keys.Count; i++)
      {
        var key = keys[i];
        var value = values[i];
        try
        {
          if (value is SpeckleObjectGroup group)
            @base[key] =
              Converter != null
                ? group.Value.Select(item => Utilities.TryConvertItemToSpeckle(item, Converter)).ToList()
                : group.Value;
          else
            @base[key] = Converter != null ? Utilities.TryConvertItemToSpeckle(value, Converter) : value;
        }
        catch (Exception e)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, e.ToFormattedString());
          hasErrors = true;
        }
      }

      if (hasErrors)
        @base = null;

      return @base;
    }
    catch (Exception ex)
    {
      // If we reach this, something happened that we weren't expecting...
      SpeckleLog.Logger.Error(ex, "Failed during execution of {componentName}", this.GetType());
      AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Object could not be extended: " + ex.ToFormattedString());
      return null;
    }
  }
}
