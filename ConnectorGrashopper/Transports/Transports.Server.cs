using Grasshopper.Kernel;
using Speckle.Core.Credentials;
using Speckle.Core.Transports;
using System;
using System.Linq;

namespace ConnectorGrashopper.Transports
{
  public class ServerTransportComponent : GH_Component
  {
    public override Guid ComponentGuid { get => new Guid("2BF256D0-3638-4278-964C-0666A97A9F0E"); }

    public override GH_Exposure Exposure => GH_Exposure.primary;

    public ServerTransportComponent() : base("Server Transport", "Server", "Creates a Server Transport.", "Speckle 2", "Transports") { }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      //pManager.AddTextParameter("base path", "P", "The root folder where you want the data to be stored. Defaults to `%appdata%/Speckle/DiskTransportFiles`.", GH_ParamAccess.item);

      pManager.AddGenericParameter("account", "A", "The Speckle Account you want this transport to send to. If not provided, will fallback on your default account.", GH_ParamAccess.item);
      pManager.AddGenericParameter("stream id", "S", "The Id of the Stream you want to send data to.", GH_ParamAccess.item);

      Params.Input.ForEach(p => p.Optional = true);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddGenericParameter("server transport", "T", "The Server Transport you have created.", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
      var test = Params.Input[0].Attributes.Pivot;
      if (DA.Iteration != 0)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Cannot create multiple transports at the same time. This is an explicit guard against possibly uninteded behaviour. If you want to create another transport, please use a new component.");
        return;
      }

      string accountId = null;
      Account account = null;
      DA.GetData(0, ref accountId);

      if (accountId == null)
      {
        account = AccountManager.GetDefaultAccount();
        AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, $"Using default account {accountId}");
      }
      else
      {
        account = AccountManager.GetAccounts().FirstOrDefault(a => a.id == accountId);
        if (account == null)
        {
          // Really last ditch effort - in case people delete accounts from the manager, and the selection dropdwon is still using an outdated list.
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"The account with an id of {accountId} was not found.");
          return;
        }
      }

      string streamId = null;
      DA.GetData(1, ref streamId);

      // TODO: Note, the behaviour described below still needs to be implemented across the server, core and gh/dyn clients.
      if (streamId == null)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Because no streamid is provided, this transport will use the default stream for your account.");
      }

      var myTransport = new ServerTransport(account, streamId);
      DA.SetData(0, myTransport);
    }
  }
}
