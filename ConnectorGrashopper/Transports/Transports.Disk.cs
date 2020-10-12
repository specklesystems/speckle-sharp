using Grasshopper.Kernel;
using System;

namespace ConnectorGrashopper.Transports
{
  public class DiskTransportComponent : GH_Component
  {
    public override Guid ComponentGuid { get => new Guid("BA068B11-2BC0-4669-BC73-09CF16820659"); }

    public override GH_Exposure Exposure => GH_Exposure.secondary;

    public DiskTransportComponent() : base("Disk Transport", "Disk", "Creates a Disk Transport.", "Speckle 2", "Transports") { }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddTextParameter("base path", "P", "The root folder where you want the data to be stored. Defaults to `%appdata%/Speckle/DiskTransportFiles`.", GH_ParamAccess.item);

      Params.Input.ForEach(p => p.Optional = true);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddGenericParameter("disk transport", "T", "The Disk Transport you have created.", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
      if (DA.Iteration != 0)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Cannot create multiple transports at the same time. This is an explicit guard against possibly uninteded behaviour. If you want to create another transport, please use a new component.");
        return;
      }

      string basePath = null;
      DA.GetData(0, ref basePath);

      var myTransport = new DiskTransport.DiskTransport(basePath);

      DA.SetData(0, myTransport);
    }
  }
}
