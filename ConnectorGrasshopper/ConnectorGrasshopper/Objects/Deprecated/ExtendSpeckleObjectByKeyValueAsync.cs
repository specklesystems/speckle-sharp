using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using ConnectorGrasshopper.Extras;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using GrasshopperAsyncComponent;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Utilities = ConnectorGrasshopper.Extras.Utilities;

namespace ConnectorGrasshopper.Objects
{
  public class ExtendSpeckleObjectByKeyValueAsync : SelectKitAsyncComponentBase
  {
    public override Guid ComponentGuid => new Guid("00287364-F725-466E-9E38-FDAD270D87D3");
    protected override Bitmap Icon => Properties.Resources.ExtendSpeckleObjectByKeyValue;
    public override GH_Exposure Exposure => GH_Exposure.hidden;
    public override bool Obsolete => true;

    public ExtendSpeckleObjectByKeyValueAsync() : base("Extend Speckle Object by Key/Value", "ESO K/V",
      "Extend a current object with key/value pairs", ComponentCategories.PRIMARY_RIBBON, ComponentCategories.OBJECTS)
    {
    }
    public override void SetConverter()
    {
      base.SetConverter();
      BaseWorker = new ExtendSpeckleObjectByKeyValueWorker(this, Converter);
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddParameter(new SpeckleBaseParam("Speckle Object", "O", "Speckle object to extend.", GH_ParamAccess.list));
      pManager.AddTextParameter("Keys", "K", "List of keys", GH_ParamAccess.list);
      pManager.AddGenericParameter("Values", "V", "List of values", GH_ParamAccess.list);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddParameter(new SpeckleBaseParam("Speckle Object", "O",
        "Extended Speckle object.", GH_ParamAccess.item));
    }
  }

  public class ExtendSpeckleObjectByKeyValueWorker : WorkerInstance
  {
    private List<Base> bases;
    private List<string> keys;
    private List<object> values;
    public ISpeckleConverter Converter;

    public ExtendSpeckleObjectByKeyValueWorker(GH_Component _parent, ISpeckleConverter converter) : base(_parent)
    {
      Converter = converter;
      keys = new List<string>();
      values = new List<object>();
    }

    public override WorkerInstance Duplicate()
    {
      return new ExtendSpeckleObjectByKeyValueWorker(Parent, Converter);
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
      RuntimeMessages.Add((GH_RuntimeMessageLevel.Remark,$"Base count: {bases.Count}"));
      RuntimeMessages.Add((GH_RuntimeMessageLevel.Remark,$"Keys count: {keys.Count}"));
      RuntimeMessages.Add((GH_RuntimeMessageLevel.Remark,$"Vals count: {values.Count}"));

      int max = bases.Count;
      if (max < values.Count) max = values.Count;
      if (max < keys.Count) max = keys.Count;

      for(int i = 0; i< max; i++)
      {
        var @base = i < bases.Count ? bases[i] : bases[bases.Count - 1];
        var key = i < keys.Count ? keys[i] : keys[keys.Count - 1];
        var value = i < values.Count ? values[i] : values[values.Count - 1];
        try
        {
          if(Converter != null)
            @base[key] = Utilities.TryConvertItemToSpeckle(value, Converter);
          else
          {
            @base[key] = value;
          }
        } catch(Exception e)
        {
          RuntimeMessages.Add((GH_RuntimeMessageLevel.Warning, $"Failed to set prop {key}: {e.Message}"));
        }
      }

      Done();
    }

    List<(GH_RuntimeMessageLevel, string)> RuntimeMessages { get; set; } = new List<(GH_RuntimeMessageLevel, string)>();

    public override void SetData(IGH_DataAccess DA)
    {
      // 👉 Checking for cancellation!
      if (CancellationToken.IsCancellationRequested) return;

      // Report all conversion errors as warnings
      if(Converter != null)
        foreach (var error in Converter.Report.ConversionErrors)
        {
          Parent.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, error.Message + ": " + error.InnerException?.Message);
        }
      
      foreach (var (level, message) in RuntimeMessages)
      {
        Parent.AddRuntimeMessage(level, message);
      }
      
      DA.SetDataList(0, bases.Select(b => new GH_SpeckleBase(b)).ToList());
    }

    public override void GetData(IGH_DataAccess DA, GH_ComponentParamServer Params)
    {
      DA.DisableGapLogic();
      List<GH_SpeckleBase> ghBases = new List<GH_SpeckleBase>();
      DA.GetDataList(0, ghBases);
      DA.GetDataList(1, keys);
      DA.GetDataList(2, values);

      if (ghBases.Count ==0 || keys.Count == 0 || values.Count == 0)
      {
        return;
      }

      bases = ghBases.Select(b => b.Value.ShallowCopy()).ToList();
    }
  }
}
