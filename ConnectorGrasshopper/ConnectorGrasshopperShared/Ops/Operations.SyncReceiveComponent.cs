using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using ConnectorGrasshopper.Objects;
using ConnectorGrasshopper.Properties;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino;
using Speckle.Core.Api;
using Speckle.Core.Api.GraphQL.Enums;
using Speckle.Core.Api.GraphQL.Models;
using Speckle.Core.Credentials;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Transports;
using Utilities = ConnectorGrasshopper.Extras.Utilities;

namespace ConnectorGrasshopper.Ops;

public class SyncReceiveComponent : SelectKitTaskCapableComponentBase<Base>
{
  public SyncReceiveComponent()
    : base(
      "Synchronous Receiver",
      "SR",
      "Receive data from a Speckle server Synchronously. This will block GH untill all the data are received which can be used to safely trigger other processes downstream",
      ComponentCategories.SECONDARY_RIBBON,
      ComponentCategories.SEND_RECEIVE
    ) { }

  public StreamWrapper StreamWrapper { get; set; }
  private Client ApiClient { get; set; }
  public string ReceivedCommitId { get; set; }
  public string InputType { get; set; }
  public bool AutoReceive { get; set; }
  public override GH_Exposure Exposure => GH_Exposure.secondary | GH_Exposure.obscure;

  protected override Bitmap Icon => Resources.SynchronousReceiver;
  public override Guid ComponentGuid => new("08C7078E-C6DA-4B3B-A57D-CD291CC79B1C");
  public string LastInfoMessage { get; internal set; }
  public bool JustPastedIn { get; internal set; }
  public string ReceivedObjectId { get; internal set; }

  public override void DocumentContextChanged(GH_Document document, GH_DocumentContext context)
  {
    switch (context)
    {
      case GH_DocumentContext.Loaded:
      {
        // Will execute every time a document becomes active (from background or opening file.).
        if (StreamWrapper != null)
        {
          Task.Run(async () =>
          {
            // Ensure fresh instance of client.
            await ResetApiClient(StreamWrapper);

            // Get last commit from the branch
            var b = ApiClient
              .BranchGet(StreamWrapper.StreamId, StreamWrapper.BranchName ?? "main", 1, CancelToken)
              .Result;

            // Compare commit id's. If they don't match, notify user or fetch data if in auto mode
            if (b.commits.items[0].id != ReceivedCommitId)
            {
              HandleNewCommit();
            }
          });
        }

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

    RhinoApp.InvokeOnUiThread(
      (Action)
        delegate
        {
          if (AutoReceive)
          {
            ExpireSolution(true);
          }
          else
          {
            OnDisplayExpired(true);
          }
        }
    );
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
    ApiClient.Subscription.CreateProjectVersionsUpdatedSubscription(StreamWrapper.StreamId).Listeners +=
      ApiClient_OnVersionUpdate;
  }

  private void ApiClient_OnVersionUpdate(object sender, ProjectVersionsUpdatedMessage e)
  {
    // Break if wrapper is branch type and branch name is not equal.
    if (StreamWrapper.Type == StreamWrapperType.Branch && e.modelId != StreamWrapper.BranchName)
    {
      return;
    }

    if (e.type != ProjectVersionsUpdatedMessageType.CREATED)
    {
      return;
    }

    HandleNewCommit();
  }

  public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
  {
    base.AppendAdditionalMenuItems(menu);

    Menu_AppendSeparator(menu);

    if (InputType == "Stream" || InputType == "Branch")
    {
      var autoReceiveMi = Menu_AppendItem(
        menu,
        "Receive automatically",
        (s, e) =>
        {
          AutoReceive = !AutoReceive;
          RhinoApp.InvokeOnUiThread(
            (Action)
              delegate
              {
                OnDisplayExpired(true);
              }
          );
        },
        true,
        AutoReceive
      );
      autoReceiveMi.ToolTipText =
        "Toggle automatic receiving. If set, any upstream change will be pulled instantly. This only is applicable when receiving a stream or a branch.";
    }
    else
    {
      var autoReceiveMi = Menu_AppendItem(
        menu,
        "Automatic receiving is disabled because you have specified a direct commit."
      );
      autoReceiveMi.ToolTipText =
        "To enable automatic receiving, you need to input a stream rather than a specific commit.";
    }
  }

  public override bool Write(GH_IWriter writer)
  {
    writer.SetBoolean("AutoReceive", AutoReceive);
    writer.SetString("LastInfoMessage", LastInfoMessage);
    writer.SetString("ReceivedObjectId", ReceivedObjectId);
    writer.SetString("ReceivedCommitId", ReceivedCommitId);

    var streamUrl = StreamWrapper != null ? StreamWrapper.ToString() : "";
    writer.SetString("StreamWrapper", streamUrl);

    return base.Write(writer);
  }

  public override bool Read(GH_IReader reader)
  {
    AutoReceive = reader.GetBoolean("AutoReceive");
    LastInfoMessage = reader.GetString("LastInfoMessage");
    ReceivedObjectId = reader.GetString("ReceivedObjectId");
    ReceivedCommitId = reader.GetString("ReceivedCommitId");

    var swString = reader.GetString("StreamWrapper");
    if (!string.IsNullOrEmpty(swString))
    {
      StreamWrapper = new StreamWrapper(swString);
    }

    JustPastedIn = true;
    return base.Read(reader);
  }

  protected override void RegisterInputParams(GH_InputParamManager pManager)
  {
    pManager.AddGenericParameter(
      "Stream",
      "S",
      "The Speckle Stream to receive data from. You can also input the Stream ID or it's URL as text.",
      GH_ParamAccess.item
    );
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
      try
      {
        ParseInput(DA);
      }
      catch (Exception e)
      {
        Console.WriteLine(e);
        throw;
      }
      if (InputType == "Invalid")
      {
        return;
      }
    }

    if (InPreSolve)
    {
      var task = Task.Run(
        async () =>
        {
          if (StreamWrapper == null)
          {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Input cannot be null");
            return null;
          }

          var acc = await StreamWrapper.GetAccount().ConfigureAwait(false);
          var client = new Client(acc);
          var remoteTransport = new ServerTransport(acc, StreamWrapper.StreamId) { TransportName = "R" };

          var myCommit = await ReceiveComponentWorker
            .GetCommit(StreamWrapper, client, AddRuntimeMessage, CancelToken)
            .ConfigureAwait(false);

          if (myCommit == null)
          {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Couldn't get the commit");
            return null;
          }

          var workspaceId = await client.GetWorkspaceId(StreamWrapper.StreamId).ConfigureAwait(false);

          Tracker.TrackNodeReceive(
            acc,
            AutoReceive,
            myCommit.authorId != acc.userInfo.id,
            myCommit.sourceApplication,
            workspaceId
          );

          var totalObjectCount = 1;

          var receivedObject = Operations
            .Receive(
              myCommit.referencedObject,
              CancelToken,
              remoteTransport,
              new SQLiteTransport { TransportName = "LC" }, // Local cache!
              null,
              null,
              count => totalObjectCount = count,
              true
            )
            .Result;

          try
          {
            await client
              .CommitReceived(
                new CommitReceivedInput
                {
                  streamId = StreamWrapper.StreamId,
                  commitId = myCommit.id,
                  message = myCommit.message,
                  sourceApplication = Utilities.GetVersionedAppName()
                }
              )
              .ConfigureAwait(false);
          }
          catch (Exception e) when (!e.IsFatal())
          {
            SpeckleLog.Logger.Error(e, "CommitReceived failed after send.");
          }

          return receivedObject;
        },
        CancelToken
      );
      TaskList.Add(task);
      return;
    }

    if (CancelToken.IsCancellationRequested)
    {
      AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Run out of time!");
    }
    else if (!GetSolveResults(DA, out Base @base))
    {
      AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Not running multithread");
    }
    else
    {
      if (@base == null)
      {
        return;
      }

      ReceivedObjectId = @base.id;

      //the active document may have changed
      Converter?.SetContextDocument(Loader.GetCurrentDocument());

      var data = Utilities.ConvertToTree(Converter, @base, AddRuntimeMessage, true);

      DA.SetDataTree(0, data);
    }
  }

  private void ParseInput(IGH_DataAccess DA)
  {
    IGH_Goo ghGoo = null;
    if (!DA.GetData(0, ref ghGoo))
    {
      return;
    }

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
    {
      inputType = GetStreamTypeMessage(newWrapper);
    }

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

    if (StreamWrapper != null && StreamWrapper.Equals(wrapper) && !JustPastedIn)
    {
      return;
    }

    StreamWrapper = wrapper;

    Task.Run(async () =>
    {
      await ResetApiClient(wrapper);
    });
  }
}
