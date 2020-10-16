using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using GrasshopperAsyncComponent;
using Speckle.Core.Api;
using Speckle.Core.Models;
using System;
using System.Linq;

namespace ConnectorGrashopper.Conversion
{
  public class SerializeObject : GH_AsyncComponent
  {
    public override Guid ComponentGuid { get => new Guid("EDEBF1F4-3FC3-4E01-95DD-286FF8804EB0"); }

    protected override System.Drawing.Bitmap Icon { get => null; }

    public override GH_Exposure Exposure => GH_Exposure.secondary;

    public SerializeObject() : base("Serialize", "SRL", "Serializes a Speckle object to a JSON string", "Speckle 2", "Conversion")
    {
      BaseWorker = new SerialzeWorker(this);
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddGenericParameter("O", "O", "Speckle objects you want to serialize.", GH_ParamAccess.tree);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddTextParameter("S", "S", "Serialized objects.", GH_ParamAccess.tree);
    }
  }

  public class SerialzeWorker : WorkerInstance
  {
    GH_Structure<IGH_Goo> Objects;
    GH_Structure<GH_String> ConvertedObjects;

    public SerialzeWorker(GH_Component parent) : base(parent)
    {
      Objects = new GH_Structure<IGH_Goo>();
      ConvertedObjects = new GH_Structure<GH_String>();
    }

    public override void DoWork(Action<string, double> ReportProgress, Action Done)
    {
      if (CancellationToken.IsCancellationRequested) return;

      int branchIndex = 0, completed = 0;
      foreach (var list in Objects.Branches)
      {
        var path = Objects.Paths[branchIndex];
        foreach (var item in list)
        {
          if (CancellationToken.IsCancellationRequested) return;

          object result = null;

          if (item is Grasshopper.Kernel.Types.IGH_Goo)
          {
            result = item.GetType().GetProperty("Value").GetValue(item);
          }

          if (result is Base)
          {
            var serialised = Operations.Serialize(result as Base);
            ConvertedObjects.Append(new GH_String() { Value = serialised }, Objects.Paths[branchIndex]);
          }
          else
          {
            ConvertedObjects.Append(new GH_String() { Value = null }, Objects.Paths[branchIndex]);
            Parent.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Object at {Objects.Paths[branchIndex]} is not a Speckle object.");
          }

          ReportProgress(Id, ((completed++ + 1) / (double)Objects.Count()));
        }

        branchIndex++;
      }

      Done();
    }

    public override WorkerInstance Duplicate() => new SerialzeWorker(Parent);

    public override void GetData(IGH_DataAccess DA, GH_ComponentParamServer Params)
    {
      if (CancellationToken.IsCancellationRequested) return;

      GH_Structure<IGH_Goo> _objects;
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
