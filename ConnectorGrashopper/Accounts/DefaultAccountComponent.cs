using System;
using Grasshopper.Kernel;

namespace ConnectorGrashopper.Accounts
{
    public class DefaultAccountComponent: GH_Component
    {
        private DefaultAccountComponent(): base("Default account","Default","Gets the default account on this machine.","Speckle 2","Accounts")
        {
        }

        public override Guid ComponentGuid => new Guid("43593656-11FF-4F56-A212-AD94F14F33B6");
        
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            throw new NotImplementedException();
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            throw new NotImplementedException();
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            throw new NotImplementedException();
        }
    }
}