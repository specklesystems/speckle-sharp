using Grasshopper.Kernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConnectorGrashopper.Conversion
{
  public class DeserializeObject : GH_Component
  {
    public override Guid ComponentGuid { get => new Guid("CC6E8983-C6E9-47ED-8F63-8DB7D677B997"); }

    protected override System.Drawing.Bitmap Icon { get => null; }

    public override GH_Exposure Exposure => GH_Exposure.primary;

    public DeserializeObject() : base("Deserialize", "DSRL", "Deserializes a JSON string to a Speckle object.", "Speckle 2", "Conversion")
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
