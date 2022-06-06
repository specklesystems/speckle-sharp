using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using ConnectorGrasshopper.Extras;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Logging = Speckle.Core.Logging;

namespace ConnectorGrasshopper.Streams
{
  public class StreamCreateComponent : GH_SpeckleComponent
  {
    public override Guid ComponentGuid => new Guid("722690DE-218D-45E1-9183-98B13C7F411D");

    protected override Bitmap Icon => Properties.Resources.CreateStream;
    public override GH_Exposure Exposure => GH_Exposure.primary;

    public StreamWrapper stream { get; set; } = null;

    public StreamCreateComponent() : base("Create Stream", "sCreate", "Create a new speckle stream.", ComponentCategories.PRIMARY_RIBBON,
      ComponentCategories.STREAMS)
    { }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddTextParameter("Account", "A", "Account to be used when creating the stream.", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddParameter(new SpeckleStreamParam("Stream", "S", "The created stream.", GH_ParamAccess.item));
    }

    public override bool Read(GH_IReader reader)
    {
      string serialisedStreamWrapper = null;
      reader.TryGetString("stream", ref serialisedStreamWrapper);

      if (serialisedStreamWrapper != null)
      {
        var pcs = serialisedStreamWrapper.Split(' ');
        stream = new StreamWrapper(pcs[0], pcs[2], pcs[1]);
      }

      return base.Read(reader);
    }

    public override bool Write(GH_IWriter writer)
    {
      if (stream == null)
      {
        return base.Write(writer);
      }

      var serialisedStreamWrapper = $"{stream.StreamId} {stream.ServerUrl} {stream.UserId}";
      writer.SetString("stream", serialisedStreamWrapper);
      return base.Write(writer);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
      if (DA.Iteration != 0)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Cannot create multiple streams at the same time. This is an explicit guard against possibly unintended behaviour. If you want to create another stream, please use a new component.");
        return;
      }


      string userId = null;
      Account account = null;
      DA.GetData(0, ref userId);

      if (userId == null)
      {
        //account = AccountManager.GetDefaultAccount();
        AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, $"Using default account {account}");
      }
      else
      {
        account = AccountManager.GetAccounts().FirstOrDefault(a => a.userInfo.id == userId);
        if (account == null)
        {
          // Really last ditch effort - in case people delete accounts from the manager, and the selection dropdown is still using an outdated list.
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"The user with id of {userId} was not found.");
          return;
        }
      }

      Params.Input[0].AddVolatileData(new GH_Path(0), 0, account.userInfo.id);

      if (stream != null)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Using cached stream. If you want to create a new stream, create a new component.");
        DA.SetData(0, new GH_SpeckleStream(stream));
        NickName = $"Id: {stream.StreamId}";
        MutableNickName = false;
        return;
      }

      Tracker.TrackNodeRun("Stream Create");
      
      Task.Run(async () =>
      {
        var client = new Client(account);
        try
        {
          var streamId = await client.StreamCreate(new StreamCreateInput { isPublic = false });
          stream = new StreamWrapper(
            streamId,
            account.userInfo.id,
            account.serverInfo.url
          );

          Rhino.RhinoApp.InvokeOnUiThread((Action)delegate
          {
            ExpireSolution(true);
          });
        }
        catch (Exception e)
        {
          Rhino.RhinoApp.InvokeOnUiThread((Action)delegate
          {
            ExpireSolution(false);
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Could not create stream at {account.serverInfo.url}:\n{e.Message}");
          });
        }
      });
    }
  }
}
