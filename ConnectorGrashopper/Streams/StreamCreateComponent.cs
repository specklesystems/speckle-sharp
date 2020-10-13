using System;
using Grasshopper.Kernel;

namespace ConnectorGrashopper.Streams
{
    public class StreamCreateComponent: GH_Component
    {
        public StreamCreateComponent() : base("Create Stream", "Create", "Create a new speckle stream", "Speckle 2",
            "Streams") { }

        public override Guid ComponentGuid => new Guid("722690DE-218D-45E1-9183-98B13C7F411D");
        
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Account", "A", "Account to be used when creating the stream.", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Stream ID", "ID", "Unique ID of the newly created stream",GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            throw new NotImplementedException();
        }
    }
}