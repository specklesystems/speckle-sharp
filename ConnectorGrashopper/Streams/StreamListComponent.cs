using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grasshopper.Kernel;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Logging;

namespace ConnectorGrashopper.Streams
{
    public class StreamListComponent: GH_Component
    {
        public StreamListComponent(): base("Stream List", "sList", "Lists all the streams for this account","Speckle 2", "Streams"){}
        public override Guid ComponentGuid => new Guid("BE790AF4-1834-495B-BE68-922B42FD53C7");
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Account", "A", "Account to get streams from", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Limit", "L", "Max number of streams to fetch", GH_ParamAccess.item, 10);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Streams", "S", "List of streams for the provided account.",
                GH_ParamAccess.list);
        }

        private List<Stream> streams = null;
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string accountId = null;
            var limit = 10;
            
            if (streams == null)
            {
                if (!DA.GetData(0, ref accountId)) return;
                if (!DA.GetData(1, ref limit)) return;

                Task.Run(async () =>
                {
                    var account = AccountManager.GetAccounts().FirstOrDefault(a => a.id == accountId);
                    Tracker.TrackEvent(Tracker.STREAM_LIST);
                    var client = new Client(account);
                    
                    // Save the result
                    streams = await client.StreamsGet(limit);
                    
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