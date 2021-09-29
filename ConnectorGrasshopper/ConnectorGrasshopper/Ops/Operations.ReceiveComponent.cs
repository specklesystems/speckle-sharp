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
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Transports;
using Utilities = ConnectorGrasshopper.Extras.Utilities;

namespace ConnectorGrasshopper.Ops
{
  public class ReceiveComponent : SelectKitAsyncComponentBase
  {
    public ReceiveComponent() : base("Receive", "Receive", "Receive data from a Speckle server", ComponentCategories.PRIMARY_RIBBON,
      ComponentCategories.SEND_RECEIVE)
    {
      BaseWorker = new ReceiveComponentWorker(this);
      Attributes = new ReceiveComponentAttributes(this);
    }
    
    public Client ApiClient { get; set; }

    public bool AutoReceive { get; set; }

    public override Guid ComponentGuid => new Guid("{3D07C1AC-2D05-42DF-A297-F861CCEEFBC7}");

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
    
    public override void AddedToDocument(GH_Document document)
    {
      SetDefaultKitAndConverter();
      base.AddedToDocument(document);
    }

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
                var b = ApiClient.BranchGet(BaseWorker.CancellationToken, StreamWrapper.StreamId, StreamWrapper.BranchName ?? "main", 1).Result;

                // Compare commit id's. If they don't match, notify user or fetch data if in auto mode
                if (b.commits.items[0].id != ReceivedCommitId)
                  HandleNewCommit();
                
                OnDisplayExpired(true);
              });
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
         ExpireSolution(true);
       else
         OnDisplayExpired(true);
     });
    }

    public override bool Write(GH_IWriter writer)
    {
      writer.SetBoolean("AutoReceive", AutoReceive);
      writer.SetString("CurrentComponentState", CurrentComponentState);
      
      writer.SetString("KitName", Kit?.Name);
      var streamUrl = StreamWrapper != null ? StreamWrapper.ToString() : "";
      writer.SetString("StreamWrapper", streamUrl);
      writer.SetString("LastInfoMessage", LastInfoMessage);
      writer.SetString("LastCommitDate", LastCommitDate);
      writer.SetString("ReceivedCommitId", ReceivedCommitId);
      return base.Write(writer);
    }

    public override bool Read(GH_IReader reader)
    {
      AutoReceive = reader.GetBoolean("AutoReceive");
      CurrentComponentState = reader.GetString("CurrentComponentState");
      LastInfoMessage = reader.GetString("LastInfoMessage");
      LastCommitDate = reader.GetString("LastCommitDate");
      ReceivedCommitId = reader.GetString("ReceivedCommitId");
      
      var swString = reader.GetString("StreamWrapper");
      if (!string.IsNullOrEmpty(swString)) StreamWrapper = new StreamWrapper(swString);

      JustPastedIn = true;

      var kitName = "";
      reader.TryGetString("KitName", ref kitName);

      if (kitName != "")
        try
        {
          SetConverterFromKit(kitName);
        }
        catch (Exception e)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
            $"Could not find the {kitName} kit on this machine. Do you have it installed? \n Will fallback to the default one.");
          SetDefaultKitAndConverter();
        }
      else
        SetDefaultKitAndConverter();

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
      pManager.AddGenericParameter("Data", "D", "Data received.", GH_ParamAccess.tree);
      pManager.AddTextParameter("Info", "I", "Commit information.", GH_ParamAccess.item);
    }

    protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
    {
      Menu_AppendSeparator(menu);
      Menu_AppendItem(menu, "Select the converter you want to use:",null,null,false,false);
      var kits = KitManager.GetKitsWithConvertersForApp(Applications.Rhino6);

      foreach (var kit in kits)
        Menu_AppendItem(menu, $"{kit.Name} ({kit.Description})", (s, e) => { SetConverterFromKit(kit.Name); }, true,
          kit.Name == Kit.Name);

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

      Menu_AppendSeparator(menu);

      if (CurrentComponentState == "receiving")
      {
        Menu_AppendItem(menu, "Cancel Receive", (s, e) =>
        {
          CurrentComponentState = "expired";
          RequestCancellation();
        });
      }

      base.AppendAdditionalComponentMenuItems(menu);
    }

    public void SetConverterFromKit(string kitName)
    {
      if (Kit == null) return;
      if (kitName == Kit.Name) return;

      Kit = KitManager.Kits.FirstOrDefault(k => k.Name == kitName);
      Converter = Kit.LoadConverter(Applications.Rhino6);

      Message = $"Using the {Kit.Name} Converter";
      ExpireSolution(true);
    }

    private bool foundKit;
    private void SetDefaultKitAndConverter()
    {
      try
      {
        Kit = KitManager.GetDefaultKit();
        Converter = Kit.LoadConverter(Applications.Rhino6);
        Converter.SetContextDocument(RhinoDoc.ActiveDoc);
        foundKit = true;
      }
      catch
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No default kit found on this machine.");
        foundKit = false;
      }
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
      DA.DisableGapLogic();

      if (!foundKit)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No kit found on this machine.");
        return;
      }
      // We need to call this always in here to be able to react and set events :/
      ParseInput(DA);

      if ((AutoReceive || CurrentComponentState == "primed_to_receive" || CurrentComponentState == "receiving") &&
          !JustPastedIn)
      {
        CurrentComponentState = "receiving";

        // Delegate control to parent async component.
        base.SolveInstance(DA);
        return;
      }
      
      // Force update output parameters
      // TODO: This is a hack due to the fact that GH_AsyncComponent overrides ExpireDownstreamObjects()
      // and will only propagate the call upwards to GH_Component if the private 'setData' prop  is == 1.
      // We should provide access to the non-overriden method, or a way to call Done() from inherited classes.

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
        OnDisplayExpired(true);
        Params.Output.ForEach(p => p.ExpireSolution(true));
      }
    }

    public override void DisplayProgress(object sender, ElapsedEventArgs e)
    {
      if (Workers.Count == 0) return;

      Message = "";
      var total = 0.0;
      foreach (var kvp in ProgressReports)
      {
        Message += $"{kvp.Key}: {kvp.Value}";
        //NOTE: progress set to indeterminate until the TotalChildrenCount is correct
        total += kvp.Value;
      }
      OverallProgress = total / ProgressReports.Keys.Count();

      RhinoApp.InvokeOnUiThread((Action)delegate { OnDisplayExpired(true); });
    }
    
    public override void RemovedFromDocument(GH_Document document)
    {
      RequestCancellation();
      //CleanApiClient();
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
      if (ghGoo == null) return;

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



      if (StreamWrapper != null && StreamWrapper.Equals(wrapper) && !JustPastedIn) return;
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
      if (StreamWrapper.Type == StreamWrapperType.Branch && e.branchName != StreamWrapper.BranchName) return;
      HandleNewCommit();
    }

    protected override void BeforeSolveInstance()
    {
      Tracker.TrackPageview("receive", AutoReceive ? "auto" : "manual");
      base.BeforeSolveInstance();
    }
  }

  public class ReceiveComponentWorker : WorkerInstance
  {
    private GH_Structure<IGH_Goo> DataInput;
    private Action<string, Exception> ErrorAction;

    private Action<ConcurrentDictionary<string, int>> InternalProgressAction;

    public ReceiveComponentWorker(GH_Component p) : base(p)
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
      return new ReceiveComponentWorker(Parent);
    }

    public override void GetData(IGH_DataAccess DA, GH_ComponentParamServer Params)
    {
      InputWrapper = ((ReceiveComponent)Parent).StreamWrapper;
    }

    public override void DoWork(Action<string, double> ReportProgress, Action Done)
    {
      var receiveComponent = ((ReceiveComponent)Parent);
      try
      {
        InternalProgressAction = dict =>
        {
          //NOTE: progress set to indeterminate until the TotalChildrenCount is correct
          //foreach (var kvp in dict) ReportProgress(kvp.Key, (double)kvp.Value / (TotalObjectCount + 1));
          foreach (var kvp in dict) ReportProgress(kvp.Key, (double)kvp.Value);
        };

        ErrorAction = (transportName, exception) =>
        {
          // TODO: This message condition should be removed once the `link sharing` issue is resolved server-side.
          var msg = exception.Message.Contains("401")
            ? "You don't have access to this stream/transport , or it doesn't exist."
            : exception.Message;
          RuntimeMessages.Add((GH_RuntimeMessageLevel.Error, $"{transportName}: { msg }"));
          Done();
          var asyncParent = (GH_AsyncComponent)Parent;
          asyncParent.CancellationSources.ForEach(source =>
          {
            if (source.Token != CancellationToken)
              source.Cancel();
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
        var remoteTransport = new ServerTransport(InputWrapper?.GetAccount().Result, InputWrapper?.StreamId);
        remoteTransport.TransportName = "R";

        // Means it's a copy paste of an empty non-init component; set the record and exit fast.
        if (receiveComponent.JustPastedIn && !receiveComponent.AutoReceive)
        {
          receiveComponent.JustPastedIn = false;
          return;
        }

        var t = Task.Run(async () =>
        {
          var myCommit = await GetCommit(InputWrapper, client, (level, message) =>
          {
            RuntimeMessages.Add((level, message));
            Done();
            return;
          }, CancellationToken);

          ReceivedCommit = myCommit;

          if (CancellationToken.IsCancellationRequested) return;

          ReceivedObject = await Operations.Receive(
            myCommit.referencedObject,
            CancellationToken,
            remoteTransport,
            new SQLiteTransport { TransportName = "LC" }, // Local cache!
            InternalProgressAction,
            ErrorAction,
            count => TotalObjectCount = count,
            disposeTransports: true
          );

          if (CancellationToken.IsCancellationRequested) return;

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
          }
          catch (Exception e)
          {
            OnFail(GH_RuntimeMessageLevel.Error, e.Message);
          }

          break;
        case StreamWrapperType.Object:
          myCommit = new Commit { referencedObject = InputWrapper.ObjectId };
          break;
        default:
          try
          {
            var branches = await client.StreamGetBranches(InputWrapper.StreamId);
            var mainBranch = branches.FirstOrDefault(b => b.name == (InputWrapper.BranchName ?? "main"));
            myCommit = mainBranch?.commits.items[0];
            return myCommit;
          }
          catch (Exception e)
          {
            OnFail(GH_RuntimeMessageLevel.Warning, $"Could not get any commits from the stream's '{(InputWrapper.BranchName ?? "main")}' branch.");
          }

          break;
      }

      return myCommit;
    }

    public override void SetData(IGH_DataAccess DA)
    {
      if (CancellationToken.IsCancellationRequested) return;

      foreach (var (level, message) in RuntimeMessages) Parent.AddRuntimeMessage(level, message);

      var parent = ((ReceiveComponent)Parent);

      parent.CurrentComponentState = "up_to_date";

      if (ReceivedCommit != null)
      {
        parent.LastInfoMessage =
          $"{ReceivedCommit.authorName} @ {ReceivedCommit.createdAt}: {ReceivedCommit.message} (id:{ReceivedCommit.id})";

        parent.ReceivedCommitId = ReceivedCommit.id;
      }

      parent.JustPastedIn = false;

      DA.SetData(1, parent.LastInfoMessage);

      if (ReceivedObject == null) return;

      //the active document may have changed
      var converter = parent.Converter;

      converter?.SetContextDocument(RhinoDoc.ActiveDoc);

      var tree = Utilities.ConvertToTree(converter, ReceivedObject);
      DA.SetDataTree(0, tree);
    }
  }

  public class ReceiveComponentAttributes : GH_ComponentAttributes
  {
    private bool _selected;

    public ReceiveComponentAttributes(GH_Component owner) : base(owner)
    {
    }

    private Rectangle ButtonBounds { get; set; }

    public override bool Selected
    {
      get => _selected;
      set
      {
        Owner.Params.ToList().ForEach(p => p.Attributes.Selected = value);
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

      var state = ((ReceiveComponent)Owner).CurrentComponentState;

      if (channel == GH_CanvasChannel.Objects)
      {
        if (((ReceiveComponent)Owner).AutoReceive)
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
      if (e.Button != MouseButtons.Left) return base.RespondToMouseDown(sender, e);
      if (!((RectangleF)ButtonBounds).Contains(e.CanvasLocation)) return base.RespondToMouseDown(sender, e);

      if (((ReceiveComponent)Owner).CurrentComponentState == "receiving") return GH_ObjectResponse.Handled;

      if (((ReceiveComponent)Owner).AutoReceive)
      {
        ((ReceiveComponent)Owner).AutoReceive = false;
        Owner.OnDisplayExpired(true);
        return GH_ObjectResponse.Handled;
      }

      // TODO: check if owner has null account/client, and call the reset thing SYNC 

      ((ReceiveComponent)Owner).CurrentComponentState = "primed_to_receive";
      Owner.ExpireSolution(true);
      return GH_ObjectResponse.Handled;
    }
  }
}
