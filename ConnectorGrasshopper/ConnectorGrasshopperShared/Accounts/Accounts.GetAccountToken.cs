using System;
using System.Drawing;
using System.Linq;
using ConnectorGrasshopper.Extras;
using ConnectorGrasshopper.Properties;
using Grasshopper;
using Grasshopper.Kernel;
using Speckle.Core.Credentials;

namespace ConnectorGrasshopper.Accounts;

public class Accounts_GetAccountToken : GH_SpeckleComponent
{
  public Accounts_GetAccountToken()
    : base(
      "Get Account Token",
      "sGAT",
      "Gets the account token from an account stored in Manager for Speckle",
      ComponentCategories.SECONDARY_RIBBON,
      ComponentCategories.COMPUTE
    )
  {
    SpeckleGHSettings.SettingsChanged += (_, args) =>
    {
      if (args.Key != SpeckleGHSettings.SHOW_DEV_COMPONENTS)
      {
        return;
      }

      var proxy = Instances.ComponentServer.ObjectProxies.FirstOrDefault(p => p.Guid == internalGuid);
      if (proxy == null)
      {
        return;
      }

      proxy.Exposure = internalExposure;
    };
  }

  public override GH_Exposure Exposure => internalExposure;
  internal static GH_Exposure internalExposure =>
    SpeckleGHSettings.ShowDevComponents ? GH_Exposure.secondary : GH_Exposure.hidden;
  internal static Guid internalGuid => new("A6327165-18E7-4316-9F57-2C212AC1FA27");

  protected override Bitmap Icon => Resources.AccountToken;
  public override Guid ComponentGuid => internalGuid;

  protected override void RegisterInputParams(GH_InputParamManager pManager)
  {
    pManager.AddParameter(new SpeckleAccountParam());
  }

  protected override void RegisterOutputParams(GH_OutputParamManager pManager)
  {
    pManager.AddTextParameter(
      "Auth Token",
      "T",
      "The auth token for this user that is stored in Manager",
      GH_ParamAccess.item
    );
  }

  public override void SolveInstanceWithLogContext(IGH_DataAccess DA)
  {
    Account account = null;
    if (!DA.GetData(0, ref account))
    {
      return;
    }

    DA.SetData(0, account.token);
  }
}
