using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using ConnectorGrasshopper.Extras;
using ConnectorGrasshopper.Properties;
using Grasshopper;
using Grasshopper.Kernel;
using Speckle.Core.Api;
using Speckle.Core.Credentials;

namespace ConnectorGrasshopper.Accounts;

public class StreamGetWithTokenComponent : GH_TaskCapableComponent<StreamWrapper>
{
  public StreamGetWithTokenComponent()
    : base(
      "Stream Get with Token",
      "SGetWT",
      "Returns a stream that will authenticate with a specific user by their Personal Access Token.\n TREAT EACH TOKEN AS A PASSWORD AND NEVER SHARE/SAVE IT IN THE FILE ITSELF",
      ComponentCategories.PRIMARY_RIBBON,
      ComponentCategories.COMPUTE
    )
  {
    SpeckleGHSettings.SettingsChanged += (_, args) =>
    {
      if (args.Key != SpeckleGHSettings.SHOW_DEV_COMPONENTS)
        return;

      var proxy = Instances.ComponentServer.ObjectProxies.FirstOrDefault(p => p.Guid == internalGuid);
      if (proxy == null)
        return;
      proxy.Exposure = internalExposure;
    };
  }

  public override GH_Exposure Exposure => internalExposure;
  internal static GH_Exposure internalExposure =>
    SpeckleGHSettings.ShowDevComponents ? GH_Exposure.secondary : GH_Exposure.hidden;
  internal static Guid internalGuid => new("89AC8586-9F37-4C99-9A65-9C3A029BA07D");
  public override Guid ComponentGuid => internalGuid;

  protected override Bitmap Icon => Resources.StreamGetWithToken;

  protected override void RegisterInputParams(GH_InputParamManager pManager)
  {
    pManager.AddParameter(
      new SpeckleStreamParam("Stream", "S", "A stream object of the stream to be updated.", GH_ParamAccess.item)
    );
    pManager.AddTextParameter("Auth Token", "t", "The auth token to access the account", GH_ParamAccess.item);
  }

  protected override void RegisterOutputParams(GH_OutputParamManager pManager)
  {
    pManager.AddParameter(
      new SpeckleStreamParam(
        "Stream",
        "S",
        "The stream object, with the authenticated account based on the input token.",
        GH_ParamAccess.item
      )
    );
  }

  protected override void SolveInstance(IGH_DataAccess DA)
  {
    if (InPreSolve)
    {
      GH_SpeckleStream ssp = null;
      string token = null;
      if (!DA.GetData(0, ref ssp))
        return;
      if (!DA.GetData(1, ref token))
        return;

      var sw = ssp.Value;

      var task = Task.Run(
        () =>
        {
          var acc = new Account();
          acc.token = token;
          acc.serverInfo = new ServerInfo { url = sw.ServerUrl };
          acc.userInfo = acc.Validate().Result;
          sw.SetAccount(acc);
          return sw;
        },
        CancelToken
      );
      TaskList.Add(task);
      return;
    }

    var solveResults = GetSolveResults(DA, out var streamWrapper);
    if (!solveResults)
    {
      AddRuntimeMessage(GH_RuntimeMessageLevel.Error, @"Could not fetch account");
      return;
    }

    DA.SetData(0, streamWrapper);
  }
}
