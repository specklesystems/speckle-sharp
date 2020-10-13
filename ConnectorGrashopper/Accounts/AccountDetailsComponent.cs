using System;
using Grasshopper.Kernel;

namespace ConnectorGrashopper.Accounts
{
    public class AccountDetailsComponent: GH_Component
    {
        private AccountDetailsComponent(): base("Account details","Details","Extracts details for a specific account.","Speckle 2","Accounts")
        {
        }

        public override Guid ComponentGuid => new Guid("E19B3ADB-57AD-413E-8A45-23A157FB282C");
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Account", "A", "Account to extract details from", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("ID", "id", "Account ID", GH_ParamAccess.item);
            pManager.AddGenericParameter("Is Default", "d", "True if this is the default account.", GH_ParamAccess.item);
            pManager.AddGenericParameter("Server Info", "Si", "Server information for this account.", GH_ParamAccess.item);
            pManager.AddGenericParameter("User Info", "Ui", "User information for this account.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            throw new NotImplementedException();
        }
    }
}