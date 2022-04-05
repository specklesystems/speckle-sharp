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
  public class DeserializeObject : GH_AsyncComponent
  {
    public override Guid ComponentGuid { get => new Guid("CC6E8983-C6E9-47ED-8F63-8DB7D677B997"); }

    protected override System.Drawing.Bitmap Icon => Properties.Resources.Deserialize;
    public override bool Obsolete => true;
    public override GH_Exposure Exposure => GH_Exposure.hidden;

    public DeserializeObject() : base("Deserialize", "Deserialize", "Deserializes a JSON string to a Speckle Base object.", ComponentCategories.SECONDARY_RIBBON, ComponentCategories.CONVERSION)
    {
      BaseWorker = new DeserializeWorker(this);
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddTextParameter("Json", "J", "Serialized base objects in JSON format.", GH_ParamAccess.tree);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddParameter(new SpeckleBaseParam("Base", "B", "Deserialized Speckle Base objects.", GH_ParamAccess.tree));
    }
  }

  public class DeserializeWorker : WorkerInstance
  {
    GH_Structure<GH_String> Objects;
    GH_Structure<GH_SpeckleBase> ConvertedObjects;

    public DeserializeWorker(GH_Component parent) : base(parent)
    {
      Objects = new GH_Structure<GH_String>();
      ConvertedObjects = new GH_Structure<GH_SpeckleBase>();
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

            try
            {
              var deserialized = Operations.Deserialize(item.Value);
              ConvertedObjects.Append(new GH_SpeckleBase { Value = deserialized }, path);
            }
            catch (Exception e)
            {
              // Add null to objects to respect output paths.
              ConvertedObjects.Append(null, path);
              RuntimeMessages.Add((GH_RuntimeMessageLevel.Warning, $"Cannot deserialize object at path {path}[{list.IndexOf(item)}]: {e.Message}."));
            }

            ReportProgress(Id, ((completed++ + 1) / (double)Objects.Count()));
          }

          branchIndex++;
        }

        Done();
      }
      catch (Exception e)
      {
        // If we reach this, something happened that we weren't expecting...
        Logging.Log.CaptureException(e);
        RuntimeMessages.Add((GH_RuntimeMessageLevel.Error, "Something went terribly wrong... " + e.Message));
        Parent.Message = "Error";
      }
    }

    public override WorkerInstance Duplicate() => new DeserializeWorker(Parent);

    public override void GetData(IGH_DataAccess DA, GH_ComponentParamServer Params)
    {
      if (CancellationToken.IsCancellationRequested) return;
      if (DA.Iteration == 0)
      {
        Logging.Analytics.TrackEvent(Logging.Analytics.Events.NodeRun, new Dictionary<string, object>() { { "name", "Deserialize" } });
      }


      GH_Structure<GH_String> _objects;
      DA.GetDataTree(0, out _objects);

      int branchIndex = 0;
      foreach (var list in _objects.Branches)
      {
        var path = _objects.Paths[branchIndex];
        foreach (var item in list)
        {
          if (item.IsValid) Objects.Append(item, path);
          else RuntimeMessages.Add((GH_RuntimeMessageLevel.Warning, $"Item at path {path}[{list.IndexOf(item)}][{list.IndexOf(item)}] is not valid."));
        }
        branchIndex++;
      }
    }

    List<(GH_RuntimeMessageLevel, string)> RuntimeMessages { get; set; } = new List<(GH_RuntimeMessageLevel, string)>();

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
