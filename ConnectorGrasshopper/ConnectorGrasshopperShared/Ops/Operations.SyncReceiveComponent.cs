using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ConnectorGrasshopper.Objects;
using ConnectorGrasshopper.Properties;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino;
using Speckle.Core.Api;
using Speckle.Core.Api.SubscriptionModels;
using Speckle.Core.Credentials;
using Speckle.Core.Kits;
using Speckle.Core.Transports;
using Logging = Speckle.Core.Logging;

namespace ConnectorGrasshopper.Ops
{
  public class SyncReceiveComponent : SelectKitTaskCapableComponentBase<Speckle.Core.Models.Base>
  {
    public StreamWrapper StreamWrapper { get; set; }
    private Client ApiClient { get; set; }
    public string ReceivedCommitId { get; set; }
    public string InputType { get; set; }
    public bool AutoReceive { get; set; }
    public override GH_Exposure Exposure => GH_Exposure.secondary | GH_Exposure.obscure;

    public override void DocumentContextChanged(GH_Document document, GH_DocumentContext context)
    {
      switch (context)
      {
        case GH_DocumentContext.Loaded:
          {
            // Will execute every time a document becomes active (from background or opening file.).
            if (StreamWrapper != null)
              Task.Run(async () =>
              {
                // Ensure fresh instance of client.
                await ResetApiClient(StreamWrapper);

                // Get last commit from the branch
                var b = ApiClient.BranchGet(CancelToken, StreamWrapper.StreamId, StreamWrapper.BranchName ?? "main", 1).Result;

                // Compare commit id's. If they don't match, notify user or fetch data if in auto mode
                if (b.commits.items[0].id != ReceivedCommitId)
                  HandleNewCommit();
              });
            break;
          }
        case GH_DocumentContext.Unloaded:
          // Will execute every time a document becomes inactive (in background or closing file.)
          //Correctly dispose of the client when changing documents to prevent subscription handlers being called in background.
          CleanApiClient();
          break;
      }

      base.DocumentContextChanged(document, context);
    }
    private void HandleNewCommit()
    {
      //CurrentComponentState = "expired";
      AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, $"There is a newer commit available for this {InputType}");

      RhinoApp.InvokeOnUiThread((Action)delegate
      {
        if (AutoReceive)
          ExpireSolution(true);
        else
          OnDisplayExpired(true);
      });
    }
    private void CleanApiClient()
    {
      ApiClient?.Dispose();
    }
    private async Task ResetApiClient(StreamWrapper wrapper)
    {
      ApiClient?.Dispose();
      var acc = await wrapper.GetAccount();
      ApiClient = new Client(acc);
      ApiClient.SubscribeCommitCreated(StreamWrapper.StreamId);
      ApiClient.OnCommitCreated += ApiClient_OnCommitCreated;
    }

    private void ApiClient_OnCommitCreated(object sender, CommitInfo e)
    {
      // Break if wrapper is branch type and branch name is not equal.
      if (StreamWrapper.Type == StreamWrapperType.Branch && e.branchName != StreamWrapper.BranchName) return;
      HandleNewCommit();
    }

    public SyncReceiveComponent() : base("Synchronous Receiver", "SR", "Receive data from a Speckle server Synchronously. This will block GH untill all the data are received which can be used to safely trigger other processes downstream",
      ComponentCategories.SECONDARY_RIBBON, ComponentCategories.SEND_RECEIVE)
    {
    }

    public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
    {

      base.AppendAdditionalMenuItems(menu);

      Menu_AppendSeparator(menu);

      if (InputType == "Stream" || InputType == "Branch")
      {
        var autoReceiveMi = Menu_AppendItem(menu, "Receive automatically", (s, e) =>
        {
          AutoReceive = !AutoReceive;
          RhinoApp.InvokeOnUiThread((Action)delegate { OnDisplayExpired(true); });
        }, true, AutoReceive);
        autoReceiveMi.ToolTipText =
          "Toggle automatic receiving. If set, any upstream change will be pulled instantly. This only is applicable when receiving a stream or a branch.";
      }
      else
      {
        var autoReceiveMi = Menu_AppendItem(menu,
          "Automatic receiving is disabled because you have specified a direct commit.");
        autoReceiveMi.ToolTipText =
          "To enable automatic receiving, you need to input a stream rather than a specific commit.";
      }
    }

    public override bool Write(GH_IWriter writer)
    {
      writer.SetBoolean("AutoReceive", AutoReceive);
      //writer.SetString("CurrentComponentState", CurrentComponentState);
      writer.SetString("LastInfoMessage", LastInfoMessage);
      //writer.SetString("LastCommitDate", LastCommitDate);
      writer.SetString("ReceivedObjectId", ReceivedObjectId);
      writer.SetString("ReceivedCommitId", ReceivedCommitId);
      writer.SetString("KitName", Kit.Name);
      var streamUrl = StreamWrapper != null ? StreamWrapper.ToString() : "";
      writer.SetString("StreamWrapper", streamUrl);
      //writer.SetBoolean(nameof(ConvertToNative), ConvertToNative);
      return base.Write(writer);
    }
    public override bool Read(GH_IReader reader)
    {
      AutoReceive = reader.GetBoolean("AutoReceive");
      //CurrentComponentState = reader.GetString("CurrentComponentState");
      LastInfoMessage = reader.GetString("LastInfoMessage");
      //LastCommitDate = reader.GetString("LastCommitDate");
      ReceivedObjectId = reader.GetString("ReceivedObjectId");
      ReceivedCommitId = reader.GetString("ReceivedCommitId");

      var swString = reader.GetString("StreamWrapper");
      if (!string.IsNullOrEmpty(swString)) StreamWrapper = new StreamWrapper(swString);

      JustPastedIn = true;
      return base.Read(reader);
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddGenericParameter("Stream", "S", "The Speckle Stream to receive data from. You can also input the Stream ID or it's URL as text.", GH_ParamAccess.item);
    }
    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddGenericParameter("Data", "D", "Data received.", GH_ParamAccess.tree);
      pManager.AddTextParameter("Info", "I", "Commit information.", GH_ParamAccess.item);
    }

    public override void SolveInstanceWithLogContext(IGH_DataAccess DA)
    {
      if (RunCount == 1)
      {
        ParseInput(DA);
        if (InputType == "Invalid") return;
      }

      if (InPreSolve)
      {

        var task = Task.Run(async () =>
        {
          var acc = await StreamWrapper?.GetAccount();
          var client = new Client(acc);
          var remoteTransport = new ServerTransport(acc, StreamWrapper?.StreamId);
          remoteTransport.TransportName = "R";


          var myCommit = await ReceiveComponentWorker.GetCommit(StreamWrapper, client, (level, message) =>
          {
            AddRuntimeMessage(level, message);
          }, CancelToken);

          if (myCommit == null)
          {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Couldn't get the commit");
            return null;
          }

          Logging.Analytics.TrackEvent(acc, Logging.Analytics.Events.Receive, new Dictionary<string, object>()
          {
            { "sync", true },
            { "sourceHostApp", HostApplications.GetHostAppFromString(myCommit.sourceApplication).Slug },
            { "sourceHostAppVersion", myCommit.sourceApplication }
          });

          var TotalObjectCount = 1;

          var ReceivedObject = Operations.Receive(
          myCommit.referencedObject,
          CancelToken,
          remoteTransport,
          new SQLiteTransport { TransportName = "LC" }, // Local cache!
          null,
          null,
          count => TotalObjectCount = count,
          disposeTransports: true
          ).Result;

          try
          {
            await client.CommitReceived(new CommitReceivedInput
            {
              streamId = StreamWrapper.StreamId,
              commitId = myCommit.id,
              message = myCommit.message,
              sourceApplication = Extras.Utilities.GetVersionedAppName()
            });
          }
          catch
          {
            // Do nothing!
          }

          return ReceivedObject;
        }, CancelToken);
        TaskList.Add(task);
        return;
      }

      if (CancelToken.IsCancellationRequested)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Run out of time!");
      }
      else if (!GetSolveResults(DA, out Speckle.Core.Models.Base @base))
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Not running multithread");
      }
      else
      {
        if (@base == null)
          return;
        ReceivedObjectId = @base.id;

        //the active document may have changed
        Converter?.SetContextDocument(Loader.GetCurrentDocument());


        var data = Extras.Utilities.ConvertToTree(Converter, @base, AddRuntimeMessage, true);

        DA.SetDataTree(0, data);
      }
    }

    private void ParseInput(IGH_DataAccess DA)
    {
      IGH_Goo ghGoo = null;
      if (!DA.GetData(0, ref ghGoo)) return;

      var input = ghGoo.GetType().GetProperty("Value")?.GetValue(ghGoo);

      var inputType = "Invalid";
      StreamWrapper newWrapper = null;

      switch (input)
      {
        case StreamWrapper wrapper:
          newWrapper = wrapper;
          break;
        case string s:
          newWrapper = new StreamWrapper(s);
          break;
      }

      if (newWrapper != null)
        inputType = GetStreamTypeMessage(newWrapper);

      InputType = inputType;
      HandleInputType(newWrapper);
    }

    private static string GetStreamTypeMessage(StreamWrapper newWrapper)
    {
      var inputType = newWrapper?.Type switch
      {
        StreamWrapperType.Undefined => "Invalid",
        StreamWrapperType.Stream => "Stream",
        StreamWrapperType.Commit => "Commit",
        StreamWrapperType.Branch => "Branch",
        _ => null
      };

      return inputType;
    }

    private void HandleInputType(StreamWrapper wrapper)
    {
      if (wrapper.Type == StreamWrapperType.Commit || wrapper.Type == StreamWrapperType.Object)
      {
        AutoReceive = false;
        StreamWrapper = wrapper;
        LastInfoMessage = null;
        return;
      }

      if (wrapper.Type == StreamWrapperType.Branch)
      {
        // NOTE: Handled in do work
      }

      if (StreamWrapper != null && StreamWrapper.Equals(wrapper) && !JustPastedIn) return;
      StreamWrapper = wrapper;

      Task.Run(async () =>
      {
        await ResetApiClient(wrapper);
      });
    }

    protected override System.Drawing.Bitmap Icon => Resources.SynchronousReceiver;
    public override Guid ComponentGuid => new Guid("08C7078E-C6DA-4B3B-A57D-CD291CC79B1C");
    public string LastInfoMessage { get; internal set; }
    public bool JustPastedIn { get; internal set; }
    public string ReceivedObjectId { get; internal set; }
  }
}
