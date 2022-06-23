using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;
using ConnectorGrasshopper.Extras;
using ConnectorGrasshopper.Objects;
using ConnectorGrasshopper.Properties;
using GH_IO.Serialization;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using GrasshopperAsyncComponent;
using Rhino;
using Speckle.Core.Api;
using Speckle.Core.Api.SubscriptionModels;
using Speckle.Core.Credentials;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Transports;
using Utilities = ConnectorGrasshopper.Extras.Utilities;

namespace ConnectorGrasshopper.Ops
{
  public class VariableInputReceiveComponent : SelectKitAsyncComponentBase, IGH_VariableParameterComponent
  {
    public VariableInputReceiveComponent() : base("Receive", "Receive", "Receive data from a Speckle server", ComponentCategories.PRIMARY_RIBBON,
      ComponentCategories.SEND_RECEIVE)
    {
      ExpandOutput = true;
      BaseWorker = new VariableInputReceiveComponentWorker(this);
      Attributes = new VariableInputReceiveComponentAttributes(this);
    }

    public Dictionary<string, GH_Structure<IGH_Goo>> PrevReceivedData;
    public Client ApiClient { get; set; }

    public bool AutoReceive { get; set; }

    public bool ReceiveOnOpen { get; set; }

    public bool ExpandOutput { get; set; }

    public override Guid ComponentGuid => new Guid("06A3E53B-2BFF-4EBD-BBCE-71B9CE36283E");

    public string CurrentComponentState { get; set; } = "needs_input";

    public override GH_Exposure Exposure => GH_Exposure.primary;

    protected override Bitmap Icon => Resources.Receiver;

    public string InputType { get; set; }

    public bool JustPastedIn { get; set; }

    public string LastCommitDate { get; set; }

    public string LastInfoMessage { get; set; }

    public double OverallProgress { get; set; }

    public string ReceivedCommitId { get; set; }

    public StreamWrapper StreamWrapper { get; set; }

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
                var b = ApiClient.BranchGet(BaseWorker.CancellationToken, StreamWrapper.StreamId, StreamWrapper.BranchName ?? "main", 1).Result;

                // Compare commit id's. If they don't match, notify user or fetch data if in auto mode
                if (b.commits.items[0].id != ReceivedCommitId)
                {
                  HandleNewCommit();
                }

                OnDisplayExpired(true);
              });
            }

            break;
          }
        case GH_DocumentContext.Unloaded:
          // Will execute every time a document becomes inactive (in background or closing file.)
          //Correctly dispose of the client when changing documents to prevent subscription handlers being called in background.
          CurrentComponentState = "expired";
          RequestCancellation();
          ApiClient?.Dispose();
          break;
      }

      base.DocumentContextChanged(document, context);
    }

    private void HandleNewCommit()
    {
      Message = "Expired";
      CurrentComponentState = "expired";
      AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, $"There is a newer commit available for this {InputType}");
      RhinoApp.InvokeOnUiThread((Action)delegate
     {
       if (AutoReceive)
       {
         ExpireSolution(true);
       }
       else
       {
         OnDisplayExpired(true);
       }
     });
    }

    public override bool Write(GH_IWriter writer)
    {
      writer.SetBoolean("AutoReceive", AutoReceive);
      writer.SetString("CurrentComponentState", CurrentComponentState);

      var streamUrl = StreamWrapper != null ? StreamWrapper.ToString() : "";
      writer.SetString("StreamWrapper", streamUrl);
      writer.SetString("LastInfoMessage", LastInfoMessage);
      writer.SetString("LastCommitDate", LastCommitDate);
      writer.SetString("ReceivedCommitId", ReceivedCommitId);
      writer.SetBoolean("ReceiveOnOpen", ReceiveOnOpen);
      writer.SetBoolean("ExpandOutput", ExpandOutput);
      return base.Write(writer);
    }

    public override bool Read(GH_IReader reader)
    {
      AutoReceive = reader.GetBoolean("AutoReceive");
      var receiveOnOpen = false;
      reader.TryGetBoolean("ReceiveOnOpen", ref receiveOnOpen);
      ReceiveOnOpen = receiveOnOpen;
      CurrentComponentState = reader.GetString("CurrentComponentState");
      LastInfoMessage = reader.GetString("LastInfoMessage");
      LastCommitDate = reader.GetString("LastCommitDate");
      ReceivedCommitId = reader.GetString("ReceivedCommitId");
      var expand = true;
      reader.TryGetBoolean("ExpandOutput", ref expand);
      ExpandOutput = expand;
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
      pManager.AddGenericParameter("Stream", "S",
        "The Speckle Stream to receive data from. You can also input the Stream ID or it's URL as text.",
        GH_ParamAccess.tree);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddTextParameter("Info", "I", "Commit information.", GH_ParamAccess.item);
      pManager.AddParameter(new SendReceiveDataParam
      {
        Name = "Data",
        NickName = "D",
        Description = "The received data.",
        Detachable = false
      });
    }

    public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
    {
      base.AppendAdditionalMenuItems(menu);

      Menu_AppendSeparator(menu);

      var noExpandMi = Menu_AppendItem(menu, "Expand commit object properties", (s, e) =>
      {
        ExpandOutput = !ExpandOutput;
        RhinoApp.InvokeOnUiThread((Action)delegate { ExpireSolution(true); });
      }, null, true, ExpandOutput);
      noExpandMi.ToolTipText = "Prevents expanding the commit object and outputs everything into the @data output.";

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

      var receivOnOpenMi = Menu_AppendItem(
        menu,
        "Receive when Document opened",
        (sender, args) =>
        {
          ReceiveOnOpen = !ReceiveOnOpen;
          RhinoApp.InvokeOnUiThread((Action)delegate { OnDisplayExpired(true); });
        },
        !AutoReceive,
        AutoReceive || ReceiveOnOpen);
      receivOnOpenMi.ToolTipText = "The node will automatically perform a receive operation as soon as the document is open, or the node is copy/pasted into a new document.";

      Menu_AppendSeparator(menu);

      if (CurrentComponentState == "receiving")
      {
        Menu_AppendItem(menu, "Cancel Receive", (s, e) =>
        {
          CurrentComponentState = "expired";
          RequestCancellation();
        });
      }

      Menu_AppendSeparator(menu);

      if (StreamWrapper != null && !string.IsNullOrEmpty(ReceivedCommitId))
        Menu_AppendItem(
          menu,
          $"View commit {ReceivedCommitId} @ {StreamWrapper.ServerUrl} online ↗",
          (s, e) => System.Diagnostics.Process.Start($"{StreamWrapper.ServerUrl}/streams/{StreamWrapper.StreamId}/commits/{ReceivedCommitId}"));
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
      DA.DisableGapLogic();

      // We need to call this always in here to be able to react and set events :/
      ParseInput(DA);

      if ((AutoReceive || CurrentComponentState == "primed_to_receive" || CurrentComponentState == "receiving") &&
          !JustPastedIn)
      {
        // if (CurrentComponentState == "primed_to_receive")
        //   Params.Output.ForEach(p => p.ExpireSolution(true));

        CurrentComponentState = "receiving";

        // Delegate control to parent async component.
        base.SolveInstance(DA);
        return;
      }

      // Set output data in a "first run" event. Note: we are not persisting the actual "sent" object as it can be very big.
      if (JustPastedIn)
      {
        // This ensures that we actually do a run. The worker will check and determine if it needs to pull an existing object or not.
        OnDisplayExpired(true);
        base.SolveInstance(DA);
      }
      else
      {
        CurrentComponentState = "expired";
        Message = "Expired";
        if (PrevReceivedData != null)
        {
          foreach (var key in PrevReceivedData.Keys)
          {
            var index = Params.Output.FindIndex(p => p.Name == key || p.NickName == key || p.Name == key.Substring(1) || p.NickName == key.Substring(1));
            var outTree = PrevReceivedData[key];
            DA.SetDataTree(index, outTree);
          }
          var infoIndex = Params.Output.FindIndex(p => p.Name == "Info" || p.NickName == "Info");
          DA.SetData(infoIndex, LastInfoMessage);

          AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Output is based on a prior receive operation. If you are seeing this message, you most likely recomputed the Grasshopper solution (F5). To ensure you have the latest data, press the Receive button again.");
        }
        OnDisplayExpired(true);
      }
    }

    public override void DisplayProgress(object sender, ElapsedEventArgs e)
    {
      if (Workers.Count == 0)
      {
        return;
      }

      Message = "";
      var total = 0.0;
      foreach (var kvp in ProgressReports)
      {
        Message += $"{kvp.Key}: {kvp.Value}";
        //NOTE: progress set to indeterminate until the TotalChildrenCount is correct
        total += kvp.Value;
      }
      OverallProgress = total / ProgressReports.Keys.Count;

      RhinoApp.InvokeOnUiThread((Action)delegate { OnDisplayExpired(true); });
    }

    public override void RemovedFromDocument(GH_Document document)
    {
      RequestCancellation();
      ApiClient?.Dispose();
      base.RemovedFromDocument(document);
    }

    private void ParseInput(IGH_DataAccess DA)
    {
      var check = DA.GetDataTree(0, out GH_Structure<IGH_Goo> DataInput);
      if (DataInput.IsEmpty)
      {
        StreamWrapper = null;
        TriggerAutoSave();
        return;
      }

      var ghGoo = DataInput.get_DataItem(0);
      if (ghGoo == null)
      {
        return;
      }

      var input = ghGoo.GetType().GetProperty("Value")?.GetValue(ghGoo);

      var inputType = "Invalid";
      StreamWrapper newWrapper = null;

      if (input is StreamWrapper wrapper)
      {
        newWrapper = wrapper;
        inputType = GetStreamTypeMessage(newWrapper);
      }
      else if (input is string s)
      {
        newWrapper = new StreamWrapper(s);
        inputType = GetStreamTypeMessage(newWrapper);
      }


      InputType = inputType;
      Message = inputType;
      HandleInputType(newWrapper);
    }

    private string GetStreamTypeMessage(StreamWrapper newWrapper)
    {
      string inputType = null;
      switch (newWrapper?.Type)
      {
        case StreamWrapperType.Undefined:
          inputType = "Invalid";
          break;
        case StreamWrapperType.Stream:
          inputType = "Stream";
          break;
        case StreamWrapperType.Commit:
          inputType = "Commit";
          break;
        case StreamWrapperType.Branch:
          inputType = "Branch";
          break;
        case StreamWrapperType.Object:
          inputType = "Object";
          break;
      }

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

      //ResetApiClient(wrapper);
      Task.Run(async () =>
      {
        await ResetApiClient(wrapper);
      });
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
      if (StreamWrapper.Type == StreamWrapperType.Branch && e.branchName != StreamWrapper.BranchName)
      {
        return;
      }

      HandleNewCommit();
    }

    public bool CanInsertParameter(GH_ParameterSide side, int index)
    {
      return false;
    }

    public bool CanRemoveParameter(GH_ParameterSide side, int index)
    {
      return false;
    }

    public IGH_Param CreateParameter(GH_ParameterSide side, int index)
    {
      var uniqueName = GH_ComponentParamServer.InventUniqueNickname("ABCD", Params.Output);
      return new SendReceiveDataParam
      {
        Name = uniqueName,
        NickName = uniqueName,
        MutableNickName = true,
        Optional = false
      };
    }

    public bool DestroyParameter(GH_ParameterSide side, int index)
    {
      return true;
    }

    public void VariableParameterMaintenance()
    {

    }
  }

  public class VariableInputReceiveComponentWorker : WorkerInstance
  {
    private GH_Structure<IGH_Goo> DataInput;
    private Action<string, Exception> ErrorAction;

    private Action<ConcurrentDictionary<string, int>> InternalProgressAction;

    public VariableInputReceiveComponentWorker(GH_Component p) : base(p)
    {
    }

    private StreamWrapper InputWrapper { get; set; }

    public Commit ReceivedCommit { get; set; }

    public Base ReceivedObject { get; set; }

    private List<(GH_RuntimeMessageLevel, string)> RuntimeMessages { get; } =
      new List<(GH_RuntimeMessageLevel, string)>();

    public int TotalObjectCount { get; set; } = 1;

    public override WorkerInstance Duplicate()
    {
      return new VariableInputReceiveComponentWorker(Parent);
    }

    public override void GetData(IGH_DataAccess DA, GH_ComponentParamServer Params)
    {
      InputWrapper = ((VariableInputReceiveComponent)Parent).StreamWrapper;
    }

    public override void DoWork(Action<string, double> ReportProgress, Action Done)
    {
      var receiveComponent = ((VariableInputReceiveComponent)Parent);
      try
      {
        InternalProgressAction = dict =>
        {
          //NOTE: progress set to indeterminate until the TotalChildrenCount is correct
          //foreach (var kvp in dict) ReportProgress(kvp.Key, (double)kvp.Value / (TotalObjectCount + 1));
          foreach (var kvp in dict)
          {
            ReportProgress(kvp.Key, kvp.Value);
          }
        };

        ErrorAction = (transportName, exception) =>
        {
          // TODO: This message condition should be removed once the `link sharing` issue is resolved server-side.
          var msg = exception.Message.Contains("401")
            ? "You don't have access to this stream/transport , or it doesn't exist."
            : exception.InnerException != null ? exception.InnerException.Message : exception.Message;
          RuntimeMessages.Add((GH_RuntimeMessageLevel.Error, $"{transportName}: { msg }"));
          Done();
          var asyncParent = (GH_AsyncComponent)Parent;
          asyncParent.CancellationSources.ForEach(source =>
          {
            if (source.Token != CancellationToken)
            {
              source.Cancel();
            }
          });
        };

        Client client;
        try
        {
          client = new Client(InputWrapper?.GetAccount().Result);
        }
        catch (Exception e)
        {
          RuntimeMessages.Add((GH_RuntimeMessageLevel.Warning, e.InnerException?.Message ?? e.Message));
          Done();
          return;
        }

        Speckle.Core.Logging.Analytics.TrackEvent(client.Account, Speckle.Core.Logging.Analytics.Events.Receive, new Dictionary<string, object>() { { "auto", receiveComponent.AutoReceive } });

        var remoteTransport = new ServerTransport(InputWrapper?.GetAccount().Result, InputWrapper?.StreamId);
        remoteTransport.TransportName = "R";

        // Means it's a copy paste of an empty non-init component; set the record and exit fast unless ReceiveOnOpen is true.
        if (receiveComponent.JustPastedIn && !receiveComponent.AutoReceive)
        {
          receiveComponent.JustPastedIn = false;
          if (!receiveComponent.ReceiveOnOpen)
            return;

          receiveComponent.CurrentComponentState = "receiving";
          RhinoApp.InvokeOnUiThread((Action)delegate { receiveComponent.OnDisplayExpired(true); });
        }

        var t = Task.Run(async () =>
        {
          ((VariableInputReceiveComponent)Parent).PrevReceivedData = null;
          var myCommit = await GetCommit(InputWrapper, client, (level, message) =>
          {
            RuntimeMessages.Add((level, message));

          }, CancellationToken);

          if (myCommit == null)
          {
            throw new Exception("Failed to find a valid commit or object to get.");
          }

          ReceivedCommit = myCommit;

          if (CancellationToken.IsCancellationRequested)
          {
            return;
          }


          ReceivedObject = await Operations.Receive(
            myCommit.referencedObject,
            CancellationToken,
            remoteTransport,
            new SQLiteTransport { TransportName = "LC" }, // Local cache!
            InternalProgressAction,
            ErrorAction,
            count => TotalObjectCount = count,
            true
          );

          try
          {
            await client.CommitReceived(new CommitReceivedInput
            {
              streamId = InputWrapper.StreamId,
              commitId = myCommit.id,
              message = myCommit.message,
              sourceApplication = Extras.Utilities.GetVersionedAppName()
            });
          }
          catch
          {
            // Do nothing!
          }

          if (CancellationToken.IsCancellationRequested)
          {
            return;
          }
          if (ReceivedObject != null)
            AutoCreateOutputs(ReceivedObject);
          Done();
        });
        t.Wait();
      }
      catch (Exception e)
      {
        // If we reach this, something happened that we weren't expecting...
        Log.CaptureException(e);
        var msg = e.InnerException?.Message ?? e.Message;
        RuntimeMessages.Add((GH_RuntimeMessageLevel.Error, msg));
        Done();
      }
    }

    public static async Task<Commit> GetCommit(StreamWrapper InputWrapper, Client client, Action<GH_RuntimeMessageLevel, string> OnFail, CancellationToken CancellationToken)
    {
      Commit myCommit = null;
      switch (InputWrapper.Type)
      {
        case StreamWrapperType.Commit:
          try
          {
            myCommit = await client.CommitGet(CancellationToken, InputWrapper.StreamId, InputWrapper.CommitId);
            return myCommit;
          }
          catch (Exception e)
          {
            OnFail(GH_RuntimeMessageLevel.Error, e.Message);
            return null;
          }
        case StreamWrapperType.Object:
          myCommit = new Commit { referencedObject = InputWrapper.ObjectId };
          return myCommit;
        case StreamWrapperType.Stream:
        case StreamWrapperType.Undefined:
          var mb = await client.BranchGet(InputWrapper.StreamId, "main", 1);
          if (mb.commits.totalCount == 0)
          {
            // TODO: Warn that we're not pulling from the main branch
            OnFail(GH_RuntimeMessageLevel.Remark, $"Main branch was empty. Defaulting to latest commit regardless of branch.");
          }
          else
          {
            return mb.commits.items[0];
          }

          var cms = await client.StreamGetCommits(InputWrapper.StreamId, 1);
          if (cms.Count == 0)
          {
            OnFail(GH_RuntimeMessageLevel.Error, $"This stream has no commits.");
            return null;
          }
          else
          {
            return cms[0];
          }
        case StreamWrapperType.Branch:
          var br = await client.BranchGet(InputWrapper.StreamId, InputWrapper.BranchName, 1);
          if (br.commits.totalCount == 0)
          {
            OnFail(GH_RuntimeMessageLevel.Error, $"This branch has no commits.");
            return null;
          }
          return br.commits.items[0];
      }

      return myCommit;
    }

    public override void SetData(IGH_DataAccess DA)
    {
      if (CancellationToken.IsCancellationRequested)
      {
        return;
      }

      foreach (var (level, message) in RuntimeMessages)
      {
        Parent.AddRuntimeMessage(level, message);
      }

      var parent = ((VariableInputReceiveComponent)Parent);

      parent.CurrentComponentState = "up_to_date";

      if (ReceivedCommit != null)
      {
        parent.LastInfoMessage =
          $"{ReceivedCommit.authorName} @ {ReceivedCommit.createdAt}: {ReceivedCommit.message} (id:{ReceivedCommit.id})";

        parent.ReceivedCommitId = ReceivedCommit.id;
      }

      parent.JustPastedIn = false;

      // Info message is always the last output.
      var infoIndex = Parent.Params.Output.FindIndex(p => p.Name == "Info");
      DA.SetData(infoIndex, parent.LastInfoMessage);

      if (ReceivedObject == null)
      {
        return;
      }


      //the active document may have changed
      var converter = parent.Converter;
      converter?.SetContextDocument(RhinoDoc.ActiveDoc);
      parent.PrevReceivedData = new Dictionary<string, GH_Structure<IGH_Goo>>();

      if (!parent.ExpandOutput || (converter != null && converter.CanConvertToNative(ReceivedObject)))
      {
        var tree = Utilities.ConvertToTree(converter, ReceivedObject, Parent.AddRuntimeMessage);
        var receiveComponent = (VariableInputReceiveComponent)this.Parent;
        receiveComponent.PrevReceivedData["Data"] = tree;
        DA.SetDataTree(1, tree);
        return;
      }

      GetOutputList(ReceivedObject).ForEach(name =>
      {
        var prop = ReceivedObject[name];
        var treeBuilder = new TreeBuilder(converter) { ConvertToNative = converter != null };
        var data = treeBuilder.Build(prop);
        var param = Parent.Params.Output.FindIndex(p => p.Name == name || p.Name == name.Substring(1));
        var ighP = Parent.Params.Output[param];
        if (ighP is SendReceiveDataParam srParam)
        {
          srParam.Detachable = name.StartsWith("@");
        }
        DA.SetDataTree(param, data);
        parent.PrevReceivedData.Add(name, data);
      });
    }

    private List<string> GetOutputList(Base b)
    {
      var receiveComponent = (VariableInputReceiveComponent)Parent;
      if (!receiveComponent.ExpandOutput || (receiveComponent.Converter != null && receiveComponent.Converter.CanConvertToNative(b)))
        return new List<string> { "Data" };

      // Get the full list of output parameters
      var fullProps = new List<string>();
      b?.GetMemberNames().ToList().ForEach(prop =>
      {
        if (!fullProps.Contains(prop))
          fullProps.Add(prop);
      });
      fullProps.Sort();
      return fullProps;
    }

    public List<string> outputList = new List<string>();

    private bool OutputMismatch() =>
      outputList.Count != Parent.Params.Output.Count
      || outputList.Where((t, i) => Parent.Params.Output[i].Name != t).Any();

    private bool HasSingleRename()
    {
      var equalLength = outputList.Count == Parent?.Params.Output.Count;
      if (!equalLength) return false;

      var diffParams = Parent?.Params.Output.Where(param => !outputList.Contains(param.Name) && !outputList.Contains("@" + param.Name));
      return diffParams.Count() == 1;
    }
    private void AutoCreateOutputs(Base @base)
    {
      outputList = GetOutputList(@base);
      outputList.Insert(0, "Info");
      if (!OutputMismatch())
        return;

      Parent.RecordUndoEvent("Creating Outputs");
      if (HasSingleRename())
      {
        var diffParams = Parent.Params.Output.Where(param => !outputList.Contains(param.Name));
        var diffOut = outputList
          .Where(name =>
            !Parent.Params.Output.Select(p => p.Name)
              .Contains(name));

        var newName = diffOut.First();
        var renameParam = diffParams.First();
        var isDetached = newName.StartsWith("@");
        var cleanName = isDetached ? newName.Substring(1) : newName;
        renameParam.NickName = cleanName;
        renameParam.Name = cleanName;
        renameParam.Description = $"Data from property: {cleanName}";
        (renameParam as SendReceiveDataParam).Detachable = isDetached;
        return;
      }

      // Check what params must be deleted, and do so when safe.
      var remove = Parent.Params.Output.Select((p, i) =>
      {
        var res = outputList.Find(o => o == p.Name || p.Name == o.Substring(1));
        return res == null ? i : -1;
      }).ToList();
      remove.Reverse();
      remove.ForEach(b =>
      {
        if (b != -1 && Parent.Params.Output[b].Recipients.Count == 0)
          Parent.Params.UnregisterOutputParameter(Parent.Params.Output[b]);
      });

      outputList.ForEach(s =>
      {
        var isDetached = s.StartsWith("@");
        var name = isDetached ? s.Substring(1) : s;
        var param = Parent.Params.Output.Find(p => p.Name == name);
        if (param == null)
        {
          var newParam = ((IGH_VariableParameterComponent)Parent).CreateParameter(GH_ParameterSide.Output, Parent.Params.Output.Count) as SendReceiveDataParam;
          newParam.Name = name;
          newParam.NickName = name;
          newParam.Description = $"Data from property: {name}";
          newParam.MutableNickName = false;
          newParam.Access = GH_ParamAccess.tree;
          newParam.Detachable = isDetached;
          Parent.Params.RegisterOutputParam(newParam, Parent.Params.Output.Count);
        }
        if (param is SendReceiveDataParam srParam)
        {
          srParam.Detachable = isDetached;
        }
      });


      var paramNames = Parent.Params.Output.Select(p => p.Name).ToList();
      paramNames.Sort();
      var sortOrder = Parent.Params.Output.Select(p => paramNames.IndexOf(p.Name)).ToArray();
      Parent.Params.SortOutput(sortOrder);
      var infoParam = Parent.Params.Output.Find(p => p.Name == "Info");
      Parent.Params.Output.Remove(infoParam);
      Parent.Params.Output.Insert(0, infoParam);
      Parent.Params.OnParametersChanged();
      ((IGH_VariableParameterComponent)Parent).VariableParameterMaintenance();
    }
  }

  public class VariableInputReceiveComponentAttributes : GH_ComponentAttributes
  {
    private bool _selected;

    public VariableInputReceiveComponentAttributes(GH_Component owner) : base(owner)
    {
    }

    private Rectangle ButtonBounds { get; set; }

    public override bool Selected
    {
      get => _selected;
      set
      {
        //Owner.Params.ToList().ForEach(p => p.Attributes.Selected = value);
        _selected = value;
      }
    }

    protected override void Layout()
    {
      base.Layout();

      var baseRec = GH_Convert.ToRectangle(Bounds);
      baseRec.Height += 26;

      var btnRec = baseRec;
      btnRec.Y = btnRec.Bottom - 26;
      btnRec.Height = 26;
      btnRec.Inflate(-2, -2);

      Bounds = baseRec;
      ButtonBounds = btnRec;
    }

    protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
    {
      base.Render(canvas, graphics, channel);

      var state = ((VariableInputReceiveComponent)Owner).CurrentComponentState;

      if (channel == GH_CanvasChannel.Objects)
      {
        if (((VariableInputReceiveComponent)Owner).AutoReceive)
        {
          var autoSendButton =
            GH_Capsule.CreateTextCapsule(ButtonBounds, ButtonBounds, GH_Palette.Blue, "Auto Receive", 2, 0);

          autoSendButton.Render(graphics, Selected, Owner.Locked, false);
          autoSendButton.Dispose();
        }
        else
        {
          var palette = state == "expired" || state == "up_to_date" ? GH_Palette.Black : GH_Palette.Transparent;
          //NOTE: progress set to indeterminate until the TotalChildrenCount is correct
          //var text = state == "receiving" ? $"{((ReceiveComponent)Owner).OverallProgress}" : "Receive";
          var text = state == "receiving" ? $"Receiving..." : "Receive";

          var button = GH_Capsule.CreateTextCapsule(ButtonBounds, ButtonBounds, palette, text, 2,
            state == "expired" ? 10 : 0);
          button.Render(graphics, Selected, Owner.Locked, false);
          button.Dispose();
        }
      }
    }

    public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
    {
      if (e.Button != MouseButtons.Left)
      {
        return base.RespondToMouseDown(sender, e);
      }

      if (!((RectangleF)ButtonBounds).Contains(e.CanvasLocation))
      {
        return base.RespondToMouseDown(sender, e);
      }

      if (((VariableInputReceiveComponent)Owner).CurrentComponentState == "receiving")
      {
        return GH_ObjectResponse.Handled;
      }

      if (((VariableInputReceiveComponent)Owner).AutoReceive)
      {
        ((VariableInputReceiveComponent)Owner).AutoReceive = false;
        Owner.OnDisplayExpired(true);
        return GH_ObjectResponse.Handled;
      }

      // TODO: check if owner has null account/client, and call the reset thing SYNC 

      ((VariableInputReceiveComponent)Owner).CurrentComponentState = "primed_to_receive";
      Owner.ExpireSolution(true);
      return GH_ObjectResponse.Handled;
    }
  }
}
