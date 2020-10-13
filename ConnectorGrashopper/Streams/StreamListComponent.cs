using System;
using Grasshopper.Kernel;

namespace ConnectorGrashopper.Streams
{
    public class StreamListComponent: GH_Component
    {
        public StreamListComponent(): base("Stream List", "sList", "Lists all the streams for this account","Speckle 2", "Streams"){}
        public override Guid ComponentGuid => new Guid("BE790AF4-1834-495B-BE68-922B42FD53C7");
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Account", "A", "Account to get streams from", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Limit", "L", "Max number of streams to fetch", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Streams", "S", "List of streams for the provided account.",
                GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            throw new NotImplementedException();
        }
    }
}