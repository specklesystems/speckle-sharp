using System;
using System.Drawing;
using Grasshopper.Kernel;
using Speckle.Core.Credentials;
using System.Linq;

namespace ConnectorGrasshopper.Accounts
{
  public class Accounts_GetAccountToken: GH_SpeckleComponent
  {
    
    public override GH_Exposure Exposure => internalExposure;
    internal static GH_Exposure internalExposure => SpeckleGHSettings.ShowDevComponents ? GH_Exposure.secondary : GH_Exposure.hidden;
    internal static Guid internalGuid => new Guid("A6327165-18E7-4316-9F57-2C212AC1FA27");

    protected override Bitmap Icon => Properties.Resources.AccountToken;
    public override Guid ComponentGuid => internalGuid;
  

    public Accounts_GetAccountToken() : base(
      "Get Account Token", 
      "sGAT", 
      "Gets the account token from an account stored in Manager for Speckle", 
      ComponentCategories.SECONDARY_RIBBON, ComponentCategories.COMPUTE)
    {
      SpeckleGHSettings.SettingsChanged += (_, args) =>
      {
        if (args.Key != SpeckleGHSettings.SHOW_DEV_COMPONENTS) return;
        
        var proxy = Grasshopper.Instances.ComponentServer.ObjectProxies.FirstOrDefault(p => p.Guid == internalGuid);
        if (proxy == null) return;
        proxy.Exposure = internalExposure;
      };
    }

    
    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddTextParameter("Account", "A", "Account to get the auth token from. Expects the `userId`", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddTextParameter("Auth Token", "T", "The auth token for this user that is stored in Manager", GH_ParamAccess.item);
    }

    public override void SolveInstanceWithLogContext(IGH_DataAccess DA)
    {
      var userId = "";
      if (!DA.GetData(0, ref userId)) return;

      var acc = AccountManager.GetAccounts().FirstOrDefault(acc => acc.userInfo.id == userId);

      DA.SetData(0, acc.token);
    }
  }
}
