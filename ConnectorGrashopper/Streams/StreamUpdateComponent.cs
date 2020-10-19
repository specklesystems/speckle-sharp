using System;
using System.Linq;
using System.Threading.Tasks;
using ConnectorGrashopper.Extras;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Speckle.Core.Api;
using Speckle.Core.Credentials;

namespace ConnectorGrashopper.Streams
{
    public class StreamUpdateComponent: GH_Component
    {
        public StreamUpdateComponent() : base("Stream Update", "sUp", "Updates a stream with new details", "Speckle 2",
            "Streams"){}
        public override Guid ComponentGuid => new Guid("F83B9956-1A5C-4844-B7F6-87A956105831");
        
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            var stream =pManager.AddParameter(new SpeckleStreamParam("Stream", "S", "Unique ID of the stream to be updated.", GH_ParamAccess.item));
            var name = pManager.AddTextParameter("Name", "N", "Name of the stream.", GH_ParamAccess.item);
            var desc = pManager.AddTextParameter("Description", "D", "Description of the stream", GH_ParamAccess.item);
            var isPublic = pManager.AddBooleanParameter("Public", "P", "True if the stream is to be publicly available.",
                GH_ParamAccess.item);
            Params.Input[name].Optional = true;
            Params.Input[desc].Optional = true;
            Params.Input[isPublic].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Stream ID", "ID", "Unique ID of the stream to be updated.", GH_ParamAccess.item);
        }

        private Stream stream;
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            StreamWrapper streamWrapper = null;
            string name = null;
            string description = null;
            bool isPublic = false;
            
            if(!DA.GetData(0, ref streamWrapper)) return;
            DA.GetData(1, ref name);
            DA.GetData(2, ref description);
            DA.GetData(3, ref isPublic);


            if (stream == null)
            {
                if (streamWrapper == null)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error,"Not a stream wrapper!");
                    return;
                }
                
                Task.Run(async () =>
                {
                    var account = streamWrapper.AccountId == null
                        ? AccountManager.GetDefaultAccount() 
                        : AccountManager.GetAccounts().FirstOrDefault(a => a.id == streamWrapper.AccountId);
                
                    var client = new Client(account);
                    var input = new StreamUpdateInput();
                    stream = await client.StreamGet(streamWrapper.StreamId);
                    input.id = streamWrapper.StreamId;
                    if (name != null && stream.name != name) input.name = name;
                    if (description != null && stream.description != description) input.description = description;
                    if (stream.isPublic != isPublic) input.isPublic = isPublic;
                    
                    try
                    {
                        var result = await client.StreamUpdate(input);
                        Rhino.RhinoApp.InvokeOnUiThread((Action) delegate
                        {
                            ExpireSolution(true);
                        });

                    }
                    catch(Exception e)
                    {
                        Rhino.RhinoApp.InvokeOnUiThread((Action) delegate
                        {
                            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, e.Message);
                        });
                    }
                    
                });
            }
            else
            {
                stream = null;
                DA.SetData(0, streamWrapper.StreamId);
            }
        }
    }
}