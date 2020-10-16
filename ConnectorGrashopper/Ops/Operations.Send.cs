using ConnectorGrashopper.Extras;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using GrasshopperAsyncComponent;
using Speckle.Core.Api;
using Speckle.Core.Models;
using Speckle.Core.Transports;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

namespace ConnectorGrashopper.Ops
{
  public class SendComponent : GH_AsyncComponent
  {
    public override Guid ComponentGuid => new Guid("{5E6A5A78-9E6F-4893-8DED-7EEAB63738A5}");

    public override GH_Exposure Exposure => GH_Exposure.primary;

    public SendComponent() : base("Send", "Send", "Sends data to a stream and creates a commit.", "Speckle 2", "Send/Receive")
    {
      BaseWorker = new SendComponentWorker(this);
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddGenericParameter("Data", "D", "A Speckle object containing the data you want to send.", GH_ParamAccess.tree);
      pManager.AddGenericParameter("Stream", "S", "Stream(s) to send to.", GH_ParamAccess.tree);
      pManager.AddTextParameter("Branch", "B", "The branch you want your commit associated with.", GH_ParamAccess.tree, "main");
      pManager.AddTextParameter("Message", "M", "Commit message.", GH_ParamAccess.tree, "");
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddTextParameter("Id", "Id", "Commit id", GH_ParamAccess.list);
      pManager.AddGenericParameter("O", "O", "Sent Object", GH_ParamAccess.list);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
      base.SolveInstance(DA);
    }

    public override void DisplayProgress(object sender, ElapsedEventArgs e)
    {
      if (Workers.Count == 0) return;
      Message = "";
      foreach (var kvp in ProgressReports)
      {
        Message += $"{kvp.Key}: {kvp.Value:0.00%}\n";
      }

      Rhino.RhinoApp.InvokeOnUiThread((Action)delegate
      {
        OnDisplayExpired(true);
      });
    }

  }

  public class SendComponentWorker : WorkerInstance
  {
    GH_Structure<IGH_Goo> DataInput;
    GH_Structure<IGH_Goo> _TransportsInput;
    GH_Structure<GH_String> _BranchNameInput;
    GH_Structure<GH_String> _MessageInput;

    string InputState;

    List<ITransport> Transports;

    Base ObjectToSend;
    long TotalObjectCount;

    Action<ConcurrentDictionary<string, int>> InternalProgressAction;

    public SendComponentWorker(GH_Component p) : base(p)
    {

    }

    public override WorkerInstance Duplicate() => new SendComponentWorker(Parent);

    public override void GetData(IGH_DataAccess DA, GH_ComponentParamServer Params)
    {
      DA.GetDataTree(0, out DataInput);
      DA.GetDataTree(1, out _TransportsInput);
      DA.GetDataTree(2, out _BranchNameInput);
      DA.GetDataTree(3, out _MessageInput);
    }

    public override void DoWork(Action<string, double> ReportProgress, Action Done)
    {
      if (CancellationToken.IsCancellationRequested) return;

      // Part 1: handle input data

      // Check wether it's a tree, or a list, or actually an item.
      // It's quite imporatant that this component only runs once! 
      InputState = "tree";
      if (DataInput.DataCount == 1)
      {
        InputState = "item";
      }
      else if (DataInput.PathCount == 1)
      {
        InputState = "list";
      }

      switch (InputState)
      {
        // Items: Easiest case: just send the base object! 
        case "item":
          ObjectToSend = ((GH_SpeckleBase)DataInput.get_DataItem(0)).Value;
          break;

        // Lists: Current convention is to wrap the list of bases in a new object, and set it as a
        // detachable subproperty called "list". See the dynamo implementation.
        case "list":
          ObjectToSend = new Base();
          ObjectToSend["@list"] = DataInput.ToList().Select(goo => ((GH_SpeckleBase)goo).Value).ToList();
          break;

        // Trees: values for each path get stored in a dictionary, where the key is the path, and the value is a list of the values inside that path. 
        case "tree":
          ObjectToSend = new Speckle.Core.Models.Base();
          var dict = new Dictionary<string, List<Base>>();
          int branchIndex = 0;
          foreach (var list in DataInput.Branches)
          {
            var path = DataInput.Paths[branchIndex];
            dict[path.ToString()] = list.Select(goo => ((GH_SpeckleBase)goo).Value).ToList();
            branchIndex++;
          }
          ObjectToSend["@dictionary"] = dict;
          break;
      }

      TotalObjectCount = ObjectToSend.GetTotalChildrenCount();

      if (CancellationToken.IsCancellationRequested) return;

      // Part 2: create transports

      Transports = new List<ITransport>();

      int t = 0;
      foreach (var data in _TransportsInput)
      {
        var transport = data.GetType().GetProperty("Value").GetValue(data);
        if (transport is StreamWrapper sw)
        {
          var acc = sw.GetAccount();
          if (acc == null)
          {
            Parent.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Could not get an account for {sw}");
            continue;
          }
          Transports.Add(new ServerTransport(acc, sw.StreamId) { TransportName = $"{sw.StreamId}@{new Uri(acc.serverInfo.url).Host}" }); ;
        } else if(transport is ITransport otherTransport)
        {
          otherTransport.TransportName = otherTransport.GetType().Name;
          Transports.Add(otherTransport);
        }
        t++;
      }

      InternalProgressAction = (dict) =>
      {
        foreach (var kvp in dict)
        {
          ReportProgress(kvp.Key, (double)kvp.Value/TotalObjectCount);
        }
      };

      if (CancellationToken.IsCancellationRequested) return;

      // Part 3: actually send stuff!

      Task.Run(async () =>
      {
        if (CancellationToken.IsCancellationRequested) return;

        // TODO: pass the cancellation token to the send ops, and downstream from there.
        var baseId = await Operations.Send(ObjectToSend, Transports, onProgressAction: InternalProgressAction);

        if (CancellationToken.IsCancellationRequested) return;

        // Create Commits
        foreach (var transport in Transports)
        {
          if (CancellationToken.IsCancellationRequested) return;
          if (!(transport is ServerTransport)) continue;

          var client = new Client(((ServerTransport)transport).Account);
          var commitId = await client.CommitCreate(new CommitCreateInput
          {
            branchName = _BranchNameInput.get_FirstItem(true).Value,
            message = _MessageInput.get_FirstItem(true).Value,
            objectId = baseId,
            streamId = ((ServerTransport)transport).StreamId
          });
        }

        if (CancellationToken.IsCancellationRequested) return;


        Done();
      });

    }

    public override void SetData(IGH_DataAccess DA)
    {
      if (CancellationToken.IsCancellationRequested) return;
      // TODO
      Parent.AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, $"Succesfully pushed {TotalObjectCount} objects.");
      DA.SetData(0, InputState);
      DA.SetData(1, new GH_SpeckleBase { Value = ObjectToSend });

    }
  }
}
