using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using ConnectorGrasshopper.Extras;
using ConnectorGrasshopper.Properties;
using Grasshopper;
using Grasshopper.Kernel;
using Speckle.Core.Credentials;

namespace ConnectorGrasshopper.Accounts;

public class ServerAccountComponent : GH_SpeckleTaskCapableComponent<Account>
{
  public ServerAccountComponent()
    : base(
      "Account from Server/Token",
      "AccST",
      "Returns an account based on a Server URL and a token. URL can be a stream url too.\n You'll need to raise a token with at least Profile:Read and Stream:Read to access your account data. Add Stream:Write to enable publishing.\n TREAT EACH TOKEN AS A PASSWORD AND NEVER SHARE/SAVE IT IN THE FILE ITSELF",
      ComponentCategories.PRIMARY_RIBBON,
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
  internal static Guid internalGuid => new("943B15DB-0A34-4A54-B10F-7FD7219954A3");
  public override Guid ComponentGuid => internalGuid;

  protected override Bitmap Icon => Resources.ServerAccount;

  protected override void RegisterInputParams(GH_InputParamManager pManager)
  {
    pManager.AddTextParameter(
      "Server",
      "S",
      "The url of the speckle server, can be a stream url too.",
      GH_ParamAccess.item
    );
    pManager.AddTextParameter("Auth Token", "t", "The auth token to access the account", GH_ParamAccess.item);
  }

  protected override void RegisterOutputParams(GH_OutputParamManager pManager)
  {
    pManager.AddParameter(new SpeckleAccountParam());
  }

  public override void SolveInstanceWithLogContext(IGH_DataAccess DA)
  {
    DA.DisableGapLogic();
    if (DA.Iteration != 0)
    {
      AddRuntimeMessage(
        GH_RuntimeMessageLevel.Warning,
        "Cannot fetch multiple accounts at the same time. This is an explicit guard against possibly unintended behaviour. If you want to get another account, please use a new component."
      );
      return;
    }

    if (InPreSolve)
    {
      string sw = null;
      string token = null;
      if (!DA.GetData(0, ref sw))
      {
        return;
      }

      if (!DA.GetData(1, ref token))
      {
        return;
      }

      if (string.IsNullOrEmpty(sw))
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Server input cannot be null");
        return;
      }

      Uri url = null;
      try
      {
        url = new Uri(sw);
      }
      catch (UriFormatException e)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Server input is not a valid url: {sw}");
        return;
      }

      var task = Task.Run(
        async () =>
        {
          var acc = new Account();
          acc.token = token;
          acc.serverInfo = await AccountManager.GetServerInfo($"{url.Scheme}://{url.Host}").ConfigureAwait(false);
          acc.userInfo = await acc.Validate().ConfigureAwait(false);
          return acc;
        },
        CancelToken
      );
      TaskList.Add(task);
      return;
    }

    if (!GetSolveResults(DA, out var account))
    {
      return;
    }

    if (account != null)
    {
      DA.SetData(0, account);
    }
  }
}
