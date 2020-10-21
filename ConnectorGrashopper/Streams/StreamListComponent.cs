using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using ConnectorGrashopper.Extras;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Logging;

namespace ConnectorGrashopper.Streams
{
  public class StreamListComponent : GH_Component
  {
    public StreamListComponent() : base("Stream List", "sList", "Lists all the streams for this account", "Speckle 2", "Streams") { }
    public override Guid ComponentGuid => new Guid("BE790AF4-1834-495B-BE68-922B42FD53C7");
    protected override Bitmap Icon => Properties.Resources.StreamList;
    
    public override GH_Exposure Exposure => GH_Exposure.primary;
    
    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      var acc = pManager.AddTextParameter("Account", "A", "Account to get streams from", GH_ParamAccess.item);
      pManager.AddIntegerParameter("Limit", "L", "Max number of streams to fetch", GH_ParamAccess.item, 10);


      Params.Input[acc].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddParameter(new SpeckleStreamParam("Streams", "S", "List of streams for the provided account.",
          GH_ParamAccess.list));
    }

    private List<StreamWrapper> streams;
    protected override void SolveInstance(IGH_DataAccess DA)
    {

      if (streams == null)
      {
        string accountId = null;
        var limit = 10;

        var account = !DA.GetData(0, ref accountId)
            ? AccountManager.GetDefaultAccount()
            : AccountManager.GetAccounts().FirstOrDefault(a => a.id == accountId);

        if (accountId == null)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "");
          return;
        }
        Params.Input[0].AddVolatileData(new GH_Path(0), 0, account.id);

        DA.GetData(1, ref limit); // Has default value so will never be empty.

        if (account == null)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No account was found.");
          return;
        }

        Task.Run(async () =>
        {
          Tracker.TrackEvent(Tracker.STREAM_LIST);
          var client = new Client(account);

                  // Save the result
                  var result = await client.StreamsGet(limit);
          streams = result.Select(stream => new StreamWrapper(stream.id, account.id, account.serverInfo.url)).ToList();
          Rhino.RhinoApp.InvokeOnUiThread((Action)delegate
                  {
              ExpireSolution(true);
            });
        });
      }
      else
      {
        DA.SetDataList(0, streams);
        streams = null;
      }
    }
  }
}