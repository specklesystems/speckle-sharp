using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ConnectorGrasshopper.Extras;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using GrasshopperAsyncComponent;
using Rhino;
using Rhino.Geometry;
using Speckle.Core.Api;
using Speckle.Core.Api.SubscriptionModels;
using Speckle.Core.Credentials;
using Speckle.Core.Kits;
using Speckle.Core.Transports;

namespace ConnectorGrasshopper.Ops
{
  public class ReceiveSync : GH_TaskCapableComponent<Speckle.Core.Models.Base>
  {
    /// <summary>
    /// Initializes a new instance of the TaskCapableMultiThread class.
    /// </summary>
    /// 
    CancellationTokenSource source;
    CancellationTokenSource tokenSource;
    const int delay = 1000;
    public WorkerInstance BaseWorker { get; set; }
    public ISpeckleKit Kit;
    public ISpeckleConverter Converter;
    public WorkerInstance currentWorker { get; set; }
    public TaskCreationOptions? TaskCreationOptions { get; set; } = null;
    public StreamWrapper StreamWrapper { get; set; }
    private Client ApiClient { get; set; }
    public string ReceivedCommitId { get; set; }
    public string CurrentComponentState { get; set; } = "needs_input";
    public string InputType { get; set; }
    public bool AutoReceive { get; set; }

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
    public void SetConverterFromKit(string kitName)
    {
      if (kitName == Kit?.Name) return;

      Kit = KitManager.Kits.FirstOrDefault(k => k.Name == kitName);
      Converter = Kit.LoadConverter(Applications.Rhino);

      Message = $"Using the {Kit.Name} Converter";
      ExpireSolution(true);
    }

    /// <summary>
    /// Initializes a new instance of the Operations class.
    /// </summary>
    public ReceiveSync() : base("ReceiveSync", "ReceiveSync", "Receive data from a Speckle server Synchronously",
      "Speckle 2", "   Send/Receive")
    {
      //BaseWorker = new ReceiveComponent2Worker(this);
      SetDefaultKitAndConverter();
    }

    protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
    {
      Menu_AppendSeparator(menu);
      Menu_AppendItem(menu, "Select the converter you want to use:");
      var kits = KitManager.GetKitsWithConvertersForApp(Applications.Rhino);

      foreach (var kit in kits)
        Menu_AppendItem(menu, $"{kit.Name} ({kit.Description})", (s, e) =>
        {
          SetConverterFromKit(kit.Name);
        }, true,
        Kit != null ?
          kit.Name == Kit.Name : false);

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

      base.AppendAdditionalComponentMenuItems(menu);
    }

    public override bool Write(GH_IWriter writer)
    {
      writer.SetBoolean("AutoReceive", AutoReceive);
      writer.SetString("CurrentComponentState", CurrentComponentState);
      writer.SetString("LastInfoMessage", LastInfoMessage);
      //writer.SetString("LastCommitDate", LastCommitDate);
      writer.SetString("ReceivedObjectId", ReceivedObjectId);
      writer.SetString("ReceivedCommitId", ReceivedCommitId);
      writer.SetString("KitName", Kit.Name);
      var streamUrl = StreamWrapper != null ? StreamWrapper.ToString() : "";
      writer.SetString("StreamWrapper", streamUrl);

      return base.Write(writer);
    }

    public override bool Read(GH_IReader reader)
    {
      AutoReceive = reader.GetBoolean("AutoReceive");
      CurrentComponentState = reader.GetString("CurrentComponentState");
      LastInfoMessage = reader.GetString("LastInfoMessage");
      //LastCommitDate = reader.GetString("LastCommitDate");
      ReceivedObjectId = reader.GetString("ReceivedObjectId");
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

    private void SetDefaultKitAndConverter()
    {
      Kit = KitManager.GetDefaultKit();
      try
      {
        Converter = Kit.LoadConverter(Applications.Rhino);
        Converter.SetContextDocument(RhinoDoc.ActiveDoc);
      }
      catch
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No default kit found on this machine.");
      }
    }


    /// <summary>
    /// Registers all the input parameters for this component.
    /// </summary>
    protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
    {
      var streamInputIndex = pManager.AddGenericParameter("Stream", "S", "The Speckle Stream to receive data from. You can also input the Stream ID or it's URL as text.", GH_ParamAccess.item);
      pManager[streamInputIndex].Optional = true;
    }

    /// <summary>
    /// Registers all the output parameters for this component.
    /// </summary>
    protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
    {
      pManager.AddGenericParameter("Data", "D", "Data received.", GH_ParamAccess.tree);
      pManager.AddTextParameter("Info", "I", "Commit information.", GH_ParamAccess.item);
    }

    /// <summary>
    /// This is the method that actually does the work.
    /// </summary>
    /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
    protected override void SolveInstance(IGH_DataAccess DA)
    {
      if (RunCount == 1)
      {
        source = new CancellationTokenSource(delay * 10);
        tokenSource = new CancellationTokenSource();
        currentWorker = BaseWorker.Duplicate();
        ParseInput(DA);
      }

      if (InPreSolve)
      {

        if (currentWorker == null)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Could not get a worker instance.");
          return;
        }

        currentWorker.GetData(DA, Params);
        currentWorker.CancellationToken = tokenSource.Token;

        var task = Task.Run(async () =>
        {
          if (StreamWrapper == null)
          {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Select a Kit");
            return null;
          }
          var acc = await StreamWrapper?.GetAccount();
          var client = new Client(acc);
          var remoteTransport = new ServerTransport(acc, StreamWrapper?.StreamId);
          remoteTransport.TransportName = "R";

          var myCommit = await ReceiveComponentWorker.GetCommit(StreamWrapper, client, (level, message) =>
          {
            AddRuntimeMessage(level, message);
            //throw new Exception(message);
          }, CancelToken);

          if (myCommit == null)
          {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Couldn't get the commit");
            return null;
          }

          var TotalObjectCount = 1;

          var ReceivedObject = Operations.Receive(
          myCommit.referencedObject,
          tokenSource.Token,
          remoteTransport,
          new SQLiteTransport { TransportName = "LC" }, // Local cache!
          null,
          null,
          count => TotalObjectCount = count
          ).Result;

          return ReceivedObject;
        }, source.Token);


        TaskList.Add(task);
        return;
      }

      if (source.IsCancellationRequested)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Run out of time!");
      }
      else if (!GetSolveResults(DA, out Speckle.Core.Models.Base ReceivedObject))
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Not running multithread");
      }
      else
      {
        if (ReceivedObject == null)
          return;
        //currentWorker.SetData(DA);
        ReceivedObjectId = ReceivedObject.id;

        //the active document may have changed
        Converter.SetContextDocument(RhinoDoc.ActiveDoc);

        // case 1: it's an item that has a direct conversion method, eg a point
        if (Converter.CanConvertToNative(ReceivedObject))
        {
          DA.SetData(0, Extras.Utilities.TryConvertItemToNative(ReceivedObject, Converter));
          Message = "Done";
          return;
        }

        var members = ReceivedObject.GetDynamicMembers();

        if (members.Count() == 1)
        {
          var treeBuilder = new TreeBuilder(Converter);
          var tree = treeBuilder.Build(ReceivedObject[members.ElementAt(0)]);

          DA.SetDataTree(0, tree);
          Message = "Done";
          return;
        }
      }
    }

    private void ParseInput(IGH_DataAccess DA)
    {
      IGH_Goo DataInput = null;
      var check = DA.GetData(0, ref DataInput);
      //if (DataInput.IsEmpty)
      //{
      //  StreamWrapper = null;
      //  TriggerAutoSave();
      //  return;
      //}

      var ghGoo = DataInput;
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

    /// <summary>
    /// Provides an Icon for the component.
    /// </summary>
    protected override System.Drawing.Bitmap Icon
    {
      get
      {
        //You can add image files to your project resources and access them like this:
        // return Resources.IconForThisComponent;
        return null;
      }
    }

    /// <summary>
    /// Gets the unique ID for this component. Do not change this ID after release.
    /// </summary>
    public override Guid ComponentGuid
    {
      get { return new Guid("a1e073a0-5203-438b-9310-e212fc81675d"); }
    }

    public string LastInfoMessage { get; internal set; }
    public bool JustPastedIn { get; internal set; }
    public string ReceivedObjectId { get; internal set; }
  }
}
