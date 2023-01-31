using System;
using System.Threading.Tasks;
using ConnectorGrasshopper.Extras;
using Grasshopper.Kernel;
using Speckle.Core.Api;
using Speckle.Core.Credentials;

namespace ConnectorGrasshopper.Accounts
{
  public class AccountFromTokenComponent: GH_TaskCapableComponent<Account>
  {
    public AccountFromTokenComponent() : base(
      "Account from Token", 
      "AFT", 
      "Returns an account from the server using an auth token", 
      ComponentCategories.PRIMARY_RIBBON, 
      ComponentCategories.ACCOUNTS)
    {
    }

    public override Guid ComponentGuid => new Guid("89AC8586-9F37-4C99-9A65-9C3A029BA07D");
    
    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddParameter(new SpeckleStreamParam("Stream", "S",
        "A stream object of the stream to be updated.", GH_ParamAccess.item));
      pManager.AddTextParameter("Auth Token", "t", "The auth token to access the account", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddGenericParameter("Account", "Acc", "The Speckle account", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
      if (InPreSolve)
      {
        GH_SpeckleStream ssp = null;
        string token = null;
        if (!DA.GetData(0, ref ssp)) return;
        if (!DA.GetData(1, ref token)) return;

        var sw = ssp.Value;
        
        var task = Task.Run(() =>
        {
          var acc = new Account();
          acc.token = token;
          acc.serverInfo = new ServerInfo { url = sw.ServerUrl };
          acc.userInfo = acc.Validate().Result;

          return acc;
        }, CancelToken);
        TaskList.Add(task);
        return;
      }

      var solveResults = GetSolveResults(DA, out var account);
      if (solveResults == false)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $@"Could not fetch account");
        return;
      }

      DA.SetData(0, account);

    }
  }
}
