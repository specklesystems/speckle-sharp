using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Speckle.Core.Models;
using System;
using System.Threading.Tasks;
using ConnectorGrasshopper.Objects;
using Grasshopper.Kernel.Data;
using Speckle.Core.Api;
using Utilities = ConnectorGrasshopper.Extras.Utilities;

namespace ConnectorGrasshopper.Ops
{
  public class SendLocalComponent : SelectKitComponentBase
  {
    public SendLocalComponent() : base("Local Send", "LS", "Sends data locally, without the need of a Speckle server", "Speckle 2", "   Send/Receive")
    {
    }

    public override Guid ComponentGuid => new Guid("6E03AC48-B9E9-48D4-886F-1197F71E4ED2");
    
    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddGenericParameter("Data", "D", "Data to send.", GH_ParamAccess.tree);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddGenericParameter("localDataId", "id", "ID of the local data sent.", GH_ParamAccess.item);
    }

    private bool hasSentObject;
    private string sentObjectId;
    protected override void SolveInstance(IGH_DataAccess DA)
    {
      if (!hasSentObject)
      {
        GH_Structure<IGH_Goo> data;
        if (!DA.GetDataTree(0, out data)) return;
      
        Task.Run(() =>
        {
          Converter.SetContextDocument(Rhino.RhinoDoc.ActiveDoc);
          var converted = Utilities.DataTreeToNestedLists(data, Converter);
          var ObjectToSend = new Base();
          ObjectToSend["@data"] = converted;
          sentObjectId = Operations.Send(ObjectToSend).Result;
          hasSentObject = true;
          Rhino.RhinoApp.InvokeOnUiThread((Action) delegate { ExpireSolution(true); });
        });
      }
      else
      {
        DA.SetData(0, sentObjectId);
        sentObjectId = null;
        hasSentObject = false;
      }
    }
  }
}
