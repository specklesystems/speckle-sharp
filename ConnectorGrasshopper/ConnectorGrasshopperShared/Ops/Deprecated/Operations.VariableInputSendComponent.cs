using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq;
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
using Speckle.Core.Credentials;
using Speckle.Core.Helpers;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Models.Extensions;
using Speckle.Core.Transports;
using Utilities = ConnectorGrasshopper.Extras.Utilities;

namespace ConnectorGrasshopper.Ops;

public class VariableInputSendComponent : SelectKitAsyncComponentBase, IGH_VariableParameterComponent
{
  private DebounceDispatcher nicknameChangeDebounce = new();

  public List<StreamWrapper> OutputWrappers = new();

  public VariableInputSendComponent()
    : base(
      "Send",
      "Send",
      "Sends data to a Speckle server (or any other provided transport).",
      ComponentCategories.PRIMARY_RIBBON,
      ComponentCategories.SEND_RECEIVE
    )
  {
    BaseWorker = new VariableInputSendComponentWorker(this);
    Attributes = new VariableInputSendComponentAttributes(this);
  }

  public override Guid ComponentGuid => new("6E528842-C478-4BD0-8DA6-30B7D1F08B04");

  protected override Bitmap Icon => Resources.Sender;
  public override bool Obsolete => true;
  public override GH_Exposure Exposure => GH_Exposure.hidden;
  public override bool CanDisableConversion => false;
  public bool AutoSend { get; set; }

  public string CurrentComponentState { get; set; } = "needs_input";

  public bool UseDefaultCache { get; set; } = true;

  public double OverallProgress { get; set; }

  public bool JustPastedIn { get; set; }

  public string BaseId { get; set; }

  public bool CanInsertParameter(GH_ParameterSide side, int index)
  {
    return side == GH_ParameterSide.Input && index >= 2;
  }

  public bool CanRemoveParameter(GH_ParameterSide side, int index)
  {
    return side == GH_ParameterSide.Input && Params.Input.Count > 3 && index >= 2;
  }

  public IGH_Param CreateParameter(GH_ParameterSide side, int index)
  {
    var uniqueName = GH_ComponentParamServer.InventUniqueNickname("ABCD", Params.Input);

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
    return side == GH_ParameterSide.Input && Params.Input.Count > 3 && index >= 2;
  }

  public void VariableParameterMaintenance() { }

  public override bool Write(GH_IWriter writer)
  {
    writer.SetBoolean("UseDefaultCache", UseDefaultCache);
    writer.SetBoolean("AutoSend", AutoSend);
    writer.SetString("CurrentComponentState", CurrentComponentState);
    writer.SetString("BaseId", BaseId);
    writer.SetString("KitName", Kit?.Name);

    var owSer = string.Join("\n", OutputWrappers.Select(ow => $"{ow.ServerUrl}\t{ow.StreamId}\t{ow.CommitId}"));
    writer.SetString("OutputWrappers", owSer);

    return base.Write(writer);
  }

  public override bool Read(GH_IReader reader)
  {
    UseDefaultCache = reader.GetBoolean("UseDefaultCache");
    AutoSend = reader.GetBoolean("AutoSend");
    CurrentComponentState = reader.GetString("CurrentComponentState");
    BaseId = reader.GetString("BaseId");

    var wrappersRaw = reader.GetString("OutputWrappers");
    var wrapperLines = wrappersRaw.Split('\n');
    if (wrapperLines != null && wrapperLines.Length != 0 && wrappersRaw != "")
    {
      foreach (var line in wrapperLines)
      {
        var pieces = line.Split('\t');
        OutputWrappers.Add(
          new StreamWrapper
          {
            ServerUrl = pieces[0],
            StreamId = pieces[1],
            CommitId = pieces[2]
          }
        );
      }

      if (OutputWrappers.Count != 0)
      {
        JustPastedIn = true;
      }
    }

    return base.Read(reader);
  }

  protected override void RegisterInputParams(GH_InputParamManager pManager)
  {
    pManager.AddGenericParameter("Stream", "S", "Stream(s) and/or transports to send to.", GH_ParamAccess.tree);
    pManager.AddTextParameter(
      "Message",
      "M",
      "Commit message. If left blank, one will be generated for you.",
      GH_ParamAccess.tree,
      ""
    );
    pManager.AddParameter(
      new SendReceiveDataParam
      {
        Name = "Data",
        NickName = "D",
        Description = "The data to send."
      }
    );
    Params.Input[1].Optional = true;
  }

  protected override void RegisterOutputParams(GH_OutputParamManager pManager)
  {
    pManager.AddGenericParameter(
      "Stream",
      "S",
      "Stream or streams pointing to the created commit",
      GH_ParamAccess.list
    );
  }

  public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
  {
    Menu_AppendSeparator(menu);

    var cacheMi = Menu_AppendItem(
      menu,
      "Use default cache",
      (s, e) => UseDefaultCache = !UseDefaultCache,
      true,
      UseDefaultCache
    );
    cacheMi.ToolTipText =
      "It's advised you always use the default cache, unless you are providing a list of custom transports and you understand the consequences.";

    var autoSendMi = Menu_AppendItem(
      menu,
      "Send automatically",
      (s, e) =>
      {
        AutoSend = !AutoSend;
        RhinoApp.InvokeOnUiThread(
          (Action)
            delegate
            {
              OnDisplayExpired(true);
            }
        );
      },
      true,
      AutoSend
    );
    autoSendMi.ToolTipText =
      "Toggle automatic data sending. If set, any change in any of the input parameters of this component will start sending.\n Please be aware that if a new send starts before an old one is finished, the previous operation is cancelled.";

    if (OutputWrappers.Count != 0)
    {
      Menu_AppendSeparator(menu);
      foreach (var ow in OutputWrappers)
      {
        Menu_AppendItem(
          menu,
          $"View commit {ow.CommitId} @ {ow.ServerUrl} online ↗",
          (s, e) => Open.Url($"{ow.ServerUrl}/streams/{ow.StreamId}/commits/{ow.CommitId}")
        );
      }
    }
    Menu_AppendSeparator(menu);

    if (CurrentComponentState == "sending")
    {
      Menu_AppendItem(
        menu,
        "Cancel Send",
        (s, e) =>
        {
          CurrentComponentState = "expired";
          RequestCancellation();
        }
      );
    }

    base.AppendAdditionalMenuItems(menu);
  }

  protected override void SolveInstance(IGH_DataAccess DA)
  {
    // Set output data in a "first run" event. Note: we are not persisting the actual "sent" object as it can be very big.
    if (JustPastedIn)
    {
      base.SolveInstance(DA);
      return;
    }

    if ((AutoSend || CurrentComponentState == "primed_to_send" || CurrentComponentState == "sending") && !JustPastedIn)
    {
      CurrentComponentState = "sending";

      // Delegate control to parent async component.
      base.SolveInstance(DA);
      return;
    }

    if (!JustPastedIn)
    {
      DA.SetDataList(0, OutputWrappers);
      //DA.SetData(1, BaseId);
      CurrentComponentState = "expired";
      Message = "Expired";
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
      Message += $"{kvp.Key}: {kvp.Value}\n";
      total += kvp.Value;
    }

    OverallProgress = total / ProgressReports.Keys.Count();

    RhinoApp.InvokeOnUiThread(
      (Action)
        delegate
        {
          OnDisplayExpired(true);
        }
    );
  }

  public override void DocumentContextChanged(GH_Document document, GH_DocumentContext context)
  {
    switch (context)
    {
      case GH_DocumentContext.Loaded:
        OnDisplayExpired(true);
        break;

      case GH_DocumentContext.Unloaded:
        // Will execute every time a document becomes inactive (in background or closing file.)
        //Correctly dispose of the client when changing documents to prevent subscription handlers being called in background.
        RequestCancellation();
        break;
    }

    base.DocumentContextChanged(document, context);
  }

  public override void AddedToDocument(GH_Document document)
  {
    base.AddedToDocument(document); // This would set the converter already.
    Params.ParameterChanged += (sender, args) =>
    {
      if (args.ParameterSide != GH_ParameterSide.Input)
      {
        return;
      }

      switch (args.OriginalArguments.Type)
      {
        case GH_ObjectEventType.NickName:
          // This means the user is typing characters, debounce until it stops for 400ms before expiring the solution.
          // Prevents UI from locking too soon while writing new names for inputs.
          args.Parameter.Name = args.Parameter.NickName;
          nicknameChangeDebounce.Debounce(400, e => ExpireSolution(true));
          break;
        case GH_ObjectEventType.NickNameAccepted:
          args.Parameter.Name = args.Parameter.NickName;
          ExpireSolution(true);
          break;
      }
    };
  }
}

[SuppressMessage(
  "Design",
  "CA1031:Do not catch general exception types",
  Justification = "Class is used by obsolete component"
)]
public class VariableInputSendComponentWorker : WorkerInstance
{
  private GH_Structure<GH_String> _MessageInput;
  private GH_Structure<IGH_Goo> _TransportsInput;
  private GH_Structure<IGH_Goo> DataInput;
  private Dictionary<string, GH_Structure<IGH_Goo>> DataInputs;

  private Action<string, Exception> ErrorAction;

  private string InputState;

  private Action<ConcurrentDictionary<string, int>> InternalProgressAction;

  private Base ObjectToSend;

  private List<StreamWrapper> OutputWrappers = new();

  private Stopwatch stopwatch;
  private long TotalObjectCount;

  private List<ITransport> Transports;

  public VariableInputSendComponentWorker(GH_Component p)
    : base(p)
  {
    RuntimeMessages = new List<(GH_RuntimeMessageLevel, string)>();
    DataInputs = new Dictionary<string, GH_Structure<IGH_Goo>>();
  }

  private List<(GH_RuntimeMessageLevel, string)> RuntimeMessages { get; set; } = new();

  public string BaseId { get; set; }

  public override WorkerInstance Duplicate()
  {
    return new VariableInputSendComponentWorker(Parent);
  }

  public override void GetData(IGH_DataAccess DA, GH_ComponentParamServer Params)
  {
    for (var i = 2; i < Params.Input.Count; i++)
    {
      DA.GetDataTree(i, out GH_Structure<IGH_Goo> input);
      DataInputs.Add(Params.Input[i].Name, input);
    }
    DA.GetDataTree(0, out _TransportsInput);
    DA.GetDataTree(1, out _MessageInput);

    OutputWrappers = new List<StreamWrapper>();
    stopwatch = new Stopwatch();
    stopwatch.Start();
  }

  public override void DoWork(Action<string, double> ReportProgress, Action Done)
  {
    try
    {
      var sendComponent = (VariableInputSendComponent)Parent;
      if (sendComponent.JustPastedIn)
      {
        Done();
        return;
      }

      if (CancellationToken.IsCancellationRequested)
      {
        sendComponent.CurrentComponentState = "expired";
        return;
      }

      //the active document may have changed
      sendComponent.Converter.SetContextDocument(Loader.GetCurrentDocument());

      // Note: this method actually converts the objects to speckle too
      ObjectToSend = new Base();
      int convertedCount = 0;

      foreach (var d in DataInputs)
      {
        try
        {
          var converted = Utilities.DataTreeToNestedLists(
            d.Value,
            sendComponent.Converter,
            CancellationToken,
            () =>
            {
              ReportProgress(
                "Conversion",
                Math.Round(convertedCount++ / (double)d.Value.DataCount / DataInputs.Count, 2)
              );
            }
          );
          var param = Parent.Params.Input.Find(p => p.Name == d.Key || p.NickName == d.Key);
          var key = d.Key;
          if (param is SendReceiveDataParam srParam)
          {
            if (srParam.Detachable && !key.StartsWith("@"))
            {
              key = "@" + key;
            }
          }

          ObjectToSend[key] = converted;
          TotalObjectCount += ObjectToSend.GetTotalChildrenCount();
        }
        catch (Exception e)
        {
          RuntimeMessages.Add((GH_RuntimeMessageLevel.Error, e.ToFormattedString()));
          Done();
          return;
        }
      }

      if (convertedCount == 0)
      {
        RuntimeMessages.Add((GH_RuntimeMessageLevel.Error, "Zero objects converted successfully. Send stopped."));
        Done();
        return;
      }
      if (CancellationToken.IsCancellationRequested)
      {
        sendComponent.CurrentComponentState = "expired";
        return;
      }

      // Part 2: create transports

      Transports = new List<ITransport>();

      if (_TransportsInput.DataCount == 0)
      {
        // TODO: Set default account + "default" user stream
      }

      var transportBranches = new Dictionary<ITransport, string>();
      int t = 0;
      foreach (var data in _TransportsInput)
      {
        var transport = data.GetType().GetProperty("Value").GetValue(data);

        if (transport is string s)
        {
          try
          {
            transport = new StreamWrapper(s);
          }
          catch (Exception e)
          {
            // TODO: Check this with team.
            RuntimeMessages.Add((GH_RuntimeMessageLevel.Warning, e.ToFormattedString()));
          }
        }

        if (transport is StreamWrapper sw)
        {
          if (sw.Type == StreamWrapperType.Undefined)
          {
            RuntimeMessages.Add((GH_RuntimeMessageLevel.Warning, "Input stream is invalid."));
            continue;
          }

          if (sw.Type == StreamWrapperType.Commit)
          {
            RuntimeMessages.Add((GH_RuntimeMessageLevel.Warning, "Cannot push to a specific commit stream url."));
            continue;
          }

          if (sw.Type == StreamWrapperType.Object)
          {
            RuntimeMessages.Add((GH_RuntimeMessageLevel.Warning, "Cannot push to a specific object stream url."));
            continue;
          }

          Account acc;
          try
          {
            acc = sw.GetAccount().Result;
          }
          catch (Exception e)
          {
            RuntimeMessages.Add((GH_RuntimeMessageLevel.Warning, e.ToFormattedString()));
            continue;
          }

          var serverTransport = new ServerTransport(acc, sw.StreamId) { TransportName = $"T{t}" };
          transportBranches.Add(serverTransport, sw.BranchName ?? "main");
          Transports.Add(serverTransport);

          sendComponent.Tracker.TrackNodeSend(acc, sendComponent.AutoSend, null);
        }
        else if (transport is ITransport otherTransport)
        {
          otherTransport.TransportName = $"T{t}";
          Transports.Add(otherTransport);
        }

        t++;
      }

      if (Transports.Count == 0)
      {
        RuntimeMessages.Add((GH_RuntimeMessageLevel.Warning, "Could not identify any valid transports to send to."));
        Done();
        return;
      }

      InternalProgressAction = dict =>
      {
        foreach (var kvp in dict)
        {
          //NOTE: progress set to indeterminate until the TotalChildrenCount is correct
          //ReportProgress(kvp.Key, (double)kvp.Value / TotalObjectCount);
          ReportProgress(kvp.Key, kvp.Value);
        }
      };

      ErrorAction = (transportName, exception) =>
      {
        // TODO: This message condition should be removed once the `link sharing` issue is resolved server-side.
        var msg = exception.Message.Contains("401")
          ? "You don't have access to this transport , or it doesn't exist."
          : exception.ToFormattedString();
        RuntimeMessages.Add((GH_RuntimeMessageLevel.Error, $"{transportName}: {msg}"));
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

      if (CancellationToken.IsCancellationRequested)
      {
        sendComponent.CurrentComponentState = "expired";
        return;
      }

      // Part 3: actually send stuff!

      var task = Task.Run(
        async () =>
        {
          if (CancellationToken.IsCancellationRequested)
          {
            sendComponent.CurrentComponentState = "expired";
            return;
          }

          // Part 3.1: persist the objects
          BaseId = await Operations.Send(
            ObjectToSend,
            CancellationToken,
            Transports,
            sendComponent.UseDefaultCache,
            InternalProgressAction,
            ErrorAction,
            true
          );

          // 3.2 Create commits for any server transport present

          var message = _MessageInput.get_FirstItem(true).Value;
          if (message == "")
          {
            message = $"Pushed {TotalObjectCount} elements from Grasshopper.";
          }

          var prevCommits = sendComponent.OutputWrappers;

          foreach (var transport in Transports)
          {
            if (CancellationToken.IsCancellationRequested)
            {
              sendComponent.CurrentComponentState = "expired";
              return;
            }

            if (!(transport is ServerTransport))
            {
              continue; // skip non-server transports (for now)
            }

            try
            {
              var client = new Client(((ServerTransport)transport).Account);
              var branch = transportBranches.ContainsKey(transport) ? transportBranches[transport] : "main";

              var commitCreateInput = new CommitCreateInput
              {
                branchName = branch,
                message = message,
                objectId = BaseId,
                streamId = ((ServerTransport)transport).StreamId,
                sourceApplication = Utilities.GetVersionedAppName()
              };

              // Check to see if we have a previous commit; if so set it.
              var prevCommit = prevCommits.FirstOrDefault(
                c => c.ServerUrl == client.ServerUrl && c.StreamId == ((ServerTransport)transport).StreamId
              );
              if (prevCommit != null)
              {
                commitCreateInput.parents = new List<string> { prevCommit.CommitId };
              }

              var commitId = await client.CommitCreate(commitCreateInput, CancellationToken);

              var wrapper = new StreamWrapper(
                $"{client.Account.serverInfo.url}/streams/{((ServerTransport)transport).StreamId}/commits/{commitId}?u={client.Account.userInfo.id}"
              );
              OutputWrappers.Add(wrapper);
            }
            catch (Exception e)
            {
              ErrorAction.Invoke("Commits", e);
            }
          }

          if (CancellationToken.IsCancellationRequested)
          {
            sendComponent.CurrentComponentState = "expired";
            Done();
          }

          Done();
        },
        CancellationToken
      );
    }
    catch (Exception ex)
    {
      // If we reach this, something happened that we weren't expecting...
      SpeckleLog.Logger.Error(ex, "Failed during execution of {componentName}", this.GetType());
      RuntimeMessages.Add((GH_RuntimeMessageLevel.Error, "Something went terribly wrong... " + ex.ToFormattedString()));
      //Parent.Message = "Error";
      //((SendComponent)Parent).CurrentComponentState = "expired";
      Done();
    }
  }

  public override void SetData(IGH_DataAccess DA)
  {
    stopwatch.Stop();

    if (((VariableInputSendComponent)Parent).JustPastedIn)
    {
      ((VariableInputSendComponent)Parent).JustPastedIn = false;
      DA.SetDataList(0, ((VariableInputSendComponent)Parent).OutputWrappers);
      return;
    }

    if (CancellationToken.IsCancellationRequested)
    {
      ((VariableInputSendComponent)Parent).CurrentComponentState = "expired";
      return;
    }

    foreach (var (level, message) in RuntimeMessages)
    {
      Parent.AddRuntimeMessage(level, message);
    }

    DA.SetDataList(0, OutputWrappers);

    ((VariableInputSendComponent)Parent).CurrentComponentState = "up_to_date";
    ((VariableInputSendComponent)Parent).OutputWrappers = OutputWrappers; // ref the outputs in the parent too, so we can serialise them on write/read

    ((VariableInputSendComponent)Parent).BaseId = BaseId; // ref the outputs in the parent too, so we can serialise them on write/read

    ((VariableInputSendComponent)Parent).OverallProgress = 0;

    var hasWarnings = RuntimeMessages.Count > 0;
    if (!hasWarnings)
    {
      Parent.AddRuntimeMessage(
        GH_RuntimeMessageLevel.Remark,
        $"Successfully pushed {TotalObjectCount} objects to {(((VariableInputSendComponent)Parent).UseDefaultCache ? Transports.Count - 1 : Transports.Count)} transports."
      );
      Parent.AddRuntimeMessage(
        GH_RuntimeMessageLevel.Remark,
        $"Send duration: {stopwatch.ElapsedMilliseconds / 1000f}s"
      );
      foreach (var t in Transports)
      {
        if (!(t is ServerTransport st))
        {
          continue;
        }

        var mb = st.TotalSentBytes / 1e6;
        Parent.AddRuntimeMessage(
          GH_RuntimeMessageLevel.Remark,
          $"{t.TransportName} avg {mb / (stopwatch.ElapsedMilliseconds / 1000f):0.00} MB/s"
        );
      }
    }
  }
}

public class VariableInputSendComponentAttributes : GH_ComponentAttributes
{
  private bool _selected;

  public VariableInputSendComponentAttributes(GH_Component owner)
    : base(owner) { }

  private Rectangle ButtonBounds { get; set; }

  public override bool Selected
  {
    get => _selected;
    set =>
      //Owner.Params.ToList().ForEach(p => p.Attributes.Selected = value);
      _selected = value;
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

    var state = ((VariableInputSendComponent)Owner).CurrentComponentState;

    if (channel == GH_CanvasChannel.Objects)
    {
      if (((VariableInputSendComponent)Owner).AutoSend)
      {
        var autoSendButton = GH_Capsule.CreateTextCapsule(
          ButtonBounds,
          ButtonBounds,
          GH_Palette.Blue,
          "Auto Send",
          2,
          0
        );

        autoSendButton.Render(graphics, Selected, Owner.Locked, false);
        autoSendButton.Dispose();
      }
      else
      {
        var palette = state == "expired" || state == "up_to_date" ? GH_Palette.Black : GH_Palette.Transparent;
        //NOTE: progress set to indeterminate until the TotalChildrenCount is correct
        //var text = state == "sending" ? $"{((SendComponent)Owner).OverallProgress}" : "Send";
        var text = state == "sending" ? "Sending..." : "Send";

        var button = GH_Capsule.CreateTextCapsule(
          ButtonBounds,
          ButtonBounds,
          palette,
          text,
          2,
          state == "expired" ? 10 : 0
        );
        button.Render(graphics, Selected, Owner.Locked, false);
        button.Dispose();
      }
    }
  }

  public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
  {
    if (e.Button == MouseButtons.Left)
    {
      if (((RectangleF)ButtonBounds).Contains(e.CanvasLocation))
      {
        if (((VariableInputSendComponent)Owner).AutoSend)
        {
          ((VariableInputSendComponent)Owner).AutoSend = false;
          Owner.OnDisplayExpired(true);
          return GH_ObjectResponse.Handled;
        }
        if (((VariableInputSendComponent)Owner).CurrentComponentState == "sending")
        {
          return GH_ObjectResponse.Handled;
        }
        ((VariableInputSendComponent)Owner).CurrentComponentState = "primed_to_send";
        Owner.ExpireSolution(true);
        return GH_ObjectResponse.Handled;
      }
    }

    return base.RespondToMouseDown(sender, e);
  }
}
