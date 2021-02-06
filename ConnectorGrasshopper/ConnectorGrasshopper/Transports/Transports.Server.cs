using ConnectorGrasshopper.Extras;
using Grasshopper.Kernel;
using Speckle.Core.Credentials;
using Speckle.Core.Logging;
using Speckle.Core.Transports;
using System;
using System.Drawing;
using System.Linq;

namespace ConnectorGrasshopper.Transports
{
  public class ServerTransportComponent : GH_Component
  {
    public override Guid ComponentGuid { get => new Guid("2BF256D0-3638-4278-964C-0666A97A9F0E"); }

    protected override Bitmap Icon => Properties.Resources.ServerTransport;

    public override GH_Exposure Exposure => GH_Exposure.primary;

    public ServerTransportComponent() : base("Server Transport", "Server", "Creates a Server Transport.", ComponentCategories.SECONDARY_RIBBON, ComponentCategories.TRANSPORTS) { }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddParameter(new SpeckleStreamParam("Stream", "S", "The stream you want to send data to.", GH_ParamAccess.item));
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddGenericParameter("server transport", "T", "The Server Transport you have created.", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
      if (DA.Iteration != 0)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Cannot create multiple transports at the same time. This is an explicit guard against possibly unintended behaviour. If you want to create another transport, please use a new component.");
        return;
      }

      GH_SpeckleStream speckleStream = null;
      StreamWrapper streamWrapper = null;
      DA.GetData(0, ref speckleStream);

      if (speckleStream == null)
      {
        return;
      }

      streamWrapper = speckleStream.Value;

      var accountId = streamWrapper.AccountId;
      Account account = null;

      account = AccountManager.GetAccounts().FirstOrDefault(a => a.id == accountId);
      if (account == null)
      {
        account = AccountManager.GetAccounts(streamWrapper.ServerUrl).FirstOrDefault();
        AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Original account not found. Please make sure you have permissions to access this stream!");
      }
      if (account == null)
      {
        ClearRuntimeMessages();
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"No account found for {streamWrapper.ServerUrl}.");
        return;
      }

      var myTransport = new ServerTransport(account, streamWrapper.StreamId);

      DA.SetData(0, myTransport);
    }

    protected override void BeforeSolveInstance()
    {
      Tracker.TrackPageview("transports", "server");
      base.BeforeSolveInstance();
    }

  }
}
