using Grasshopper.Kernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConnectorGrashopper.Conversion
{
  public class SerializeObject : GH_Component
  {
    public override Guid ComponentGuid { get => new Guid("191FE5ED-3FCA-4391-858F-36DB27812167"); }

    protected override System.Drawing.Bitmap Icon { get => null; }

    public override GH_Exposure Exposure => GH_Exposure.primary;

    public SerializeObject() : base("Serialize", "SRL", "Serializes a Speckle object to a JSON string", "Speckle 2", "Conversion")
    {

    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddGenericParameter("O", "O", "Speckle objects you want to serialize.", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddGenericParameter("S", "S", "Serialized objects.", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
      AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Not implemented yet");
    }

  }
}
