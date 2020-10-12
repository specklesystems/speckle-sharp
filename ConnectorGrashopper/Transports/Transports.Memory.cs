using Grasshopper.Kernel;
using Speckle.Core.Transports;
using System;

namespace ConnectorGrashopper.Transports
{
  public class MemoryTransportComponent : GH_Component
  {
    public override Guid ComponentGuid { get => new Guid("B3E7A1E0-FB96-45AE-9F47-54D1B495AAC9"); }

    public override GH_Exposure Exposure => GH_Exposure.secondary;

    public MemoryTransportComponent() : base("Memory Transport", "Memory", "Creates an Memory Transport.", "Speckle 2", "Transports") { }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddTextParameter("name", "N", "The name of this Memory Transport.", GH_ParamAccess.item);

      Params.Input.ForEach(p => p.Optional = true);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddGenericParameter("disk transport", "T", "The Memory Transport you have created.", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
      if (DA.Iteration != 0)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Cannot create multiple transports at the same time. This is an explicit guard against possibly uninteded behaviour. If you want to create another transport, please use a new component.");
        return;
      }
      string name = null;
      DA.GetData(0, ref name);

      var myTransport = new MemoryTransport();
      myTransport.TransportName = name;

      DA.SetData(0, myTransport);
    }
  }
}
