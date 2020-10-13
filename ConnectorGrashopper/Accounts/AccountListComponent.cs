using System;
using Grasshopper.Kernel;

namespace ConnectorGrashopper.Accounts
{
    public class AccountListComponent: GH_Component
    {
        private AccountListComponent(): base("Account List","Accounts","Lists of configured accounts in this machine.","Speckle 2","Accounts")
        {
        }

        public override Guid ComponentGuid => new Guid("1FA26124-982C-4DCB-B1F8-40BC300B7B2C");
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