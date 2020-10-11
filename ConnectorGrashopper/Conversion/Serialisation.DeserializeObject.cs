using ConnectorGrashopper.Extras;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using GrasshopperAsyncComponent;
using Speckle.Core.Api;
using System;
using System.Linq;

namespace ConnectorGrashopper.Conversion
{
  public class DeserializeObject : GH_AsyncComponent
  {
    public override Guid ComponentGuid { get => new Guid("CC6E8983-C6E9-47ED-8F63-8DB7D677B997"); }

    protected override System.Drawing.Bitmap Icon { get => null; }

    public override GH_Exposure Exposure => GH_Exposure.primary;

    public DeserializeObject() : base("Deserialize", "Deserialize", "Deserializes a JSON string to a Speckle object.", "Speckle 2", "Conversion")
    {
      BaseWorker = new DeserialzeWorker();
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddTextParameter("O", "O", "Speckle objects you want to serialize.", GH_ParamAccess.tree);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddGenericParameter("S", "S", "Serialized objects.", GH_ParamAccess.tree);
    }
  }

  public class DeserialzeWorker : WorkerInstance
  {
    GH_Structure<GH_String> Objects;
    GH_Structure<GH_SpeckleBase> ConvertedObjects;

    public DeserialzeWorker()
    {
      Objects = new GH_Structure<GH_String>();
      ConvertedObjects = new GH_Structure<GH_SpeckleBase>();
    }

    public override void DoWork(Action<string, double> ReportProgress, Action<string, GH_RuntimeMessageLevel> ReportError, Action Done)
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
            var serialised = Operations.Deserialize(item.Value);
            ConvertedObjects.Append(new GH_SpeckleBase() { Value = serialised }, Objects.Paths[branchIndex]);
          }
          catch (Exception e)
          {
            ConvertedObjects.Append(new GH_SpeckleBase() { Value = null }, Objects.Paths[branchIndex]);
            ReportError($"Object at {Objects.Paths[branchIndex]} is not a Speckle object. Exception: {e.Message}.", GH_RuntimeMessageLevel.Warning);
          }

          ReportProgress(Id, ((completed++ + 1) / (double)Objects.Count()));
        }

        branchIndex++;
      }

      Done();
    }

    public override WorkerInstance Duplicate() => new DeserialzeWorker();

    public override void GetData(IGH_DataAccess DA, GH_ComponentParamServer Params)
    {
      if (CancellationToken.IsCancellationRequested) return;

      GH_Structure<GH_String> _objects;
      DA.GetDataTree(0, out _objects);

      int branchIndex = 0;
      foreach (var list in _objects.Branches)
      {
        var path = _objects.Paths[branchIndex];
        foreach (var item in list)
        {
          Objects.Append(item, _objects.Paths[branchIndex]);
        }
        branchIndex++;
      }
    }

    public override void SetData(IGH_DataAccess DA)
    {
      if (CancellationToken.IsCancellationRequested) return;
      DA.SetDataTree(0, ConvertedObjects);
    }
  }
}
