using System;
using System.Collections.Generic;
using System.Linq;
using ConnectorGrasshopper.Extras;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using GrasshopperAsyncComponent;
using Speckle.Core.Api;
using Logging = Speckle.Core.Logging;

namespace ConnectorGrasshopper.Conversion
{
  public class SerializeObject : GH_SpeckleAsyncComponent
  {
    public override Guid ComponentGuid { get => new Guid("EDEBF1F4-3FC3-4E01-95DD-286FF8804EB0"); }

    protected override System.Drawing.Bitmap Icon => Properties.Resources.Serialize;
    public override bool Obsolete => true;
    public override GH_Exposure Exposure => GH_Exposure.hidden;

    public SerializeObject() : base("Serialize", "SRL", "Serializes a Speckle Base object to JSON", ComponentCategories.SECONDARY_RIBBON, ComponentCategories.CONVERSION)
    {
      BaseWorker = new SerializeWorker(this);
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddParameter(new SpeckleBaseParam("Base", "B", "Speckle base objects to serialize.", GH_ParamAccess.tree));
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddTextParameter("Json", "J", "Serialized objects in JSON format.", GH_ParamAccess.tree);
    }
  }

  public class SerializeWorker : WorkerInstance
  {
    GH_Structure<GH_SpeckleBase> Objects;
    GH_Structure<GH_String> ConvertedObjects;

    public SerializeWorker(GH_Component parent) : base(parent)
    {
      Objects = new GH_Structure<GH_SpeckleBase>();
      ConvertedObjects = new GH_Structure<GH_String>();
    }

    public override void DoWork(Action<string, double> ReportProgress, Action Done)
    {
      try
      {
        if (CancellationToken.IsCancellationRequested) return;

        int branchIndex = 0, completed = 0;
        foreach (var list in Objects.Branches)
        {
          var path = Objects.Paths[branchIndex];
          foreach (var item in list)
          {
            if (CancellationToken.IsCancellationRequested) return;

            if (item != null && item.Value != null)
            {
              try
              {
                var serialised = Operations.Serialize(item.Value);
                ConvertedObjects.Append(new GH_String { Value = serialised }, path);
              }
              catch (Exception e)
              {
                RuntimeMessages.Add((GH_RuntimeMessageLevel.Warning, e.Message));
              }
            }
            else
            {
              ConvertedObjects.Append(null, path);
              RuntimeMessages.Add((GH_RuntimeMessageLevel.Warning, $"Item at path {{{path}}}[{list.IndexOf(item)}] is not a Base object."));
            }

            ReportProgress(Id, ((completed++ + 1) / (double)Objects.Count()));
          }

          branchIndex++;
        }

      }
      catch (Exception e)
      {
        // If we reach this, something happened that we weren't expecting...
        Logging.Log.CaptureException(e);
        RuntimeMessages.Add((GH_RuntimeMessageLevel.Error, "Something went terribly wrong... " + e.Message));
        Parent.Message = "Error";
      }
      // Always call done
      Done();
    }

    public override WorkerInstance Duplicate() => new SerializeWorker(Parent);

    List<(GH_RuntimeMessageLevel, string)> RuntimeMessages { get; set; } = new List<(GH_RuntimeMessageLevel, string)>();

    public override void GetData(IGH_DataAccess DA, GH_ComponentParamServer Params)
    {
      if (CancellationToken.IsCancellationRequested) return;
      if (DA.Iteration == 0)
       (Parent as GH_SpeckleAsyncComponent)?.Tracker.TrackNodeRun();

      GH_Structure<GH_SpeckleBase> _objects;
      DA.GetDataTree(0, out _objects);

      int branchIndex = 0;
      foreach (var list in _objects.Branches)
      {
        var path = _objects.Paths[branchIndex];
        foreach (var item in list)
        {
          Objects.Append(item, path);
        }
        branchIndex++;
      }
    }

    public override void SetData(IGH_DataAccess DA)
    {
      if (CancellationToken.IsCancellationRequested) return;
      foreach (var (level, message) in RuntimeMessages)
      {
        Parent.AddRuntimeMessage(level, message);
      }
      DA.SetDataTree(0, ConvertedObjects);
    }
  }
}
