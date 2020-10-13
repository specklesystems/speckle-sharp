using System;
using Grasshopper.Kernel;

namespace ConnectorGrashopper.Streams
{
    public class StreamUpdateComponent: GH_Component
    {
        public StreamUpdateComponent() : base("Stream Update", "sUp", "Updates a stream with new details", "Speckle 2",
            "Streams"){}
        public override Guid ComponentGuid => new Guid("F83B9956-1A5C-4844-B7F6-87A956105831");
        
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Stream ID", "ID", "Unique ID of the stream to be updated.", GH_ParamAccess.item);
            pManager.AddTextParameter("Name", "N", "Name of the stream.", GH_ParamAccess.item);
            pManager.AddTextParameter("Description", "D", "Description of the stream", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Public", "P", "True if the stream is to be publicly available.",
                GH_ParamAccess.item);
            pManager.AddGenericParameter("Account", "A", "Account to update stream.", GH_ParamAccess.item);

        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Stream ID", "ID", "Unique ID of the stream to be updated.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            throw new NotImplementedException();
        }
    }
}