using System;
using System.Linq;
using System.Threading.Tasks;
using ConnectorGrasshopper.Extras;
using Eto.Drawing;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Models.Extensions;
using Bitmap = System.Drawing.Bitmap;

namespace ConnectorGrasshopper.Streams
{
  public class StreamCreateComponentV2 : GH_SpeckleTaskCapableComponent<StreamWrapper>
  {
    public StreamWrapper stream { get; set; } = null;

    public StreamCreateComponentV2() : base("Create Stream V2", "sCreate", "Create a new speckle stream.",
      ComponentCategories.PRIMARY_RIBBON,
      ComponentCategories.STREAMS)
    {
    }

    public override Guid ComponentGuid => new Guid("8BE87947-1C12-49DE-A92E-B9CC6C2C10F7");
    protected override Bitmap Icon => Properties.Resources.CreateStream;
    public override GH_Exposure Exposure => GH_Exposure.primary;

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
      if (stream != null)
        writer.SetString("stream", $"{stream.StreamId} {stream.ServerUrl} {stream.UserId}");
      return base.Write(writer);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
      if (InPreSolve)
      {
        if (DA.Iteration == 0)
        {
          string userId = null;
          DA.GetData(0, ref userId);

          TaskList.Add(CreateStream(userId));
        }

        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
          "Cannot create multiple streams at the same time. This is an explicit guard against possibly unintended behaviour. If you want to create another stream, please use a new component.");
      }

      if (GetSolveResults(DA, out var value))
        DA.SetData(0, value);
    }

    public async Task<StreamWrapper> CreateStream(string userId)
    {
      var account = AccountManager.GetAccounts().FirstOrDefault(a => a.userInfo.id == userId);
      if (account == null)
      {
        // Really last ditch effort - in case people delete accounts from the manager, and the selection dropdown is still using an outdated list.
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"The user with id of {userId} was not found.");
        return null;
      }

      if (stream != null)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
          "Using cached stream. If you want to create a new stream, create a new component.");
        return stream;
      }

      Tracker.TrackNodeRun("Stream Create");

      var client = new Client(account);

      var streamId = await client.StreamCreate(new StreamCreateInput { isPublic = false });
      return new StreamWrapper(
        streamId,
        account.userInfo.id,
        account.serverInfo.url
      );
    }
  }
}
