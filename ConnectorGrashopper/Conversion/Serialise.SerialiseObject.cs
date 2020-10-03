using ConnectorGrashopper.Extras;
using Grasshopper.Kernel;
using Speckle.Core.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConnectorGrashopper.Conversion
{
  public class SerializeObject : GH_Component
  {
    public override Guid ComponentGuid { get => new Guid("EDEBF1F4-3FC3-4E01-95DD-286FF8804EB0"); }

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
      pManager.AddTextParameter("S", "S", "Serialized objects.", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
      GH_SpeckleBase obj = null;
      DA.GetData(0, ref obj);

      if (obj == null) return;

      var text = Operations.Serialize(obj.Value);
      DA.SetData(0, text);

      //AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Not implemented yet");
    }

  }
}
