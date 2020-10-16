using System;
using System.Collections.Generic;
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
    public class StreamGetComponent: GH_Component
    {
        public StreamGetComponent(): base("Stream Get", "sGet", "Gets a specific stream from your account","Speckle 2", "Streams"){}
        
        public override Guid ComponentGuid => new Guid("D66AFB58-A1BA-487C-94BF-AF0FFFBA6CE5");
        
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Stream ID", "ID", "Stream ID to fetch stream from the server", GH_ParamAccess.item);
            var acc = pManager.AddTextParameter("Account", "A", "Account to get stream with.", GH_ParamAccess.item);

            Params.Input[acc].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new SpeckleStreamParam("Stream", "S", "Speckle Stream",
                GH_ParamAccess.item));
        }

        private StreamWrapper stream;
        
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string accountId = null;
            string id = null;

            DA.GetData(0, ref id);
            var account = !DA.GetData(1, ref accountId) 
                ? AccountManager.GetDefaultAccount() 
                : AccountManager.GetAccounts().FirstOrDefault(a => a.id == accountId);
            
            Params.Input[1].AddVolatileData(new GH_Path(0), 0, account.id);

            if (stream == null)
            {
                // Validation
                string errorMessage = null;
                if (!ValidateInput(account, id, ref errorMessage))
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error,errorMessage);
                    return;
                }
                // Run
                Task.Run(async () =>
                {
                    try
                    {
                        Tracker.TrackEvent(Tracker.STREAM_LIST);
                        var client = new Client(account);
                        
                        //Exists?
                        var result = await client.StreamGet(id);

                        stream = new StreamWrapper(result.id,account.id,account.serverInfo.url);
                        Rhino.RhinoApp.InvokeOnUiThread((Action)delegate
                        {
                            ExpireSolution(true);
                        });
                        
                    }
                    catch (Exception e)
                    {
                        stream = null;
                        Rhino.RhinoApp.InvokeOnUiThread((Action)delegate
                        {
                            AddRuntimeMessage(GH_RuntimeMessageLevel.Error,e.Message);
                        });
                    }
                });
            }
            else
            {
                DA.SetData(0, stream);
                stream = null;
            }
        }

        private bool ValidateInput(Account account, string id, ref string s)
        {
            string message = null;
            if (account == null)
            { s="No account was found.";
                return false;
            }

            if (id == null)
            {
                s = "ID cannot be null";
                return false;
            }

            if (!IsValidId(id, ref message))
            {
                s = message;
                return false;
            }

            s = null;
            return true;
        }

        private bool IsValidId(string id, ref string s)
        {
            // TODO: Add validation!
            return true;
        }
    }
}