using ConnectorGrashopper.Extras;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ConnectorGrashopper.Streams
{
  public class StreamCreateComponent : GH_Component
  {
    public override Guid ComponentGuid => new Guid("722690DE-218D-45E1-9183-98B13C7F411D");

    public StreamWrapper Stream { get; set; } = null;

    public StreamCreateComponent() : base("Create Stream", "Create", "Create a new speckle stream", "Speckle 2",
        "Streams")
    { }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddTextParameter("Account", "A", "Account to be used when creating the stream.", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddGenericParameter("Stream", "S", "The created stream.", GH_ParamAccess.item);
    }

    public override bool Read(GH_IReader reader)
    {
      var serialisedStreamWrapper = reader.GetString("stream");

      if (serialisedStreamWrapper != null)
      {
        var pcs = serialisedStreamWrapper.Split(' ');
        Stream = new StreamWrapper { StreamId = pcs[0], ServerUrl = pcs[1], AccountId = pcs[2] };
      }

      return base.Read(reader);
    }

    public override bool Write(GH_IWriter writer)
    {
      if (Stream == null)
      {
        return base.Write(writer);
      }

      var serialisedStreamWrapper = $"{Stream.StreamId} {Stream.ServerUrl} {Stream.AccountId}";
      writer.SetString("stream", serialisedStreamWrapper);
      return base.Write(writer);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
      if (DA.Iteration != 0)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Cannot create multiple streams at the same time. This is an explicit guard against possibly uninteded behaviour. If you want to create another stream, please use a new component.");
        return;
      }

      if (Stream != null)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, $"Using cached stream. If you want to create a new stream, create a new component.");
        DA.SetData(0, Stream);
        NickName = $"Id: {Stream.StreamId}";
        MutableNickName = false;
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

      Task.Run(async () =>
      {
        var client = new Client(account);
        var streamId = await client.StreamCreate(new StreamCreateInput());
        Stream = new StreamWrapper
        {
          AccountId = account.id,
          ServerUrl = account.serverInfo.url,
          StreamId = streamId
        };
        Rhino.RhinoApp.InvokeOnUiThread((Action)delegate
        {
          ExpireSolution(true);
        });
      });
    }
  }
}