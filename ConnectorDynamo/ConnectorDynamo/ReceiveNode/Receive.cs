extern alias DynamoNewtonsoft;
using DNJ = DynamoNewtonsoft::Newtonsoft.Json;
using Dynamo.Engine;
using Dynamo.Graph.Nodes;
using ProtoCore.AST.AssociativeAST;
using Speckle.Core.Api;
using Speckle.Core.Api.SubscriptionModels;
using Speckle.Core.Credentials;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Speckle.Core.Logging;
using System.Collections.Concurrent;
using Dynamo.Utilities;
using Speckle.ConnectorDynamo.Functions;
using System.Threading;
using System.Threading.Tasks;

namespace Speckle.ConnectorDynamo.ReceiveNode
{
  /// <summary>
  /// Receive data from Speckle
  /// </summary>
  [NodeName("Receive")]
  [NodeCategory("Speckle 2")]
  [NodeDescription("Receive data from a Speckle server")]
  [NodeSearchTags("receive", "speckle")]
  [IsDesignScriptCompatible]
  public class Receive : NodeModel
  {
    internal event Action OnRequestUpdates;

    protected virtual void RequestUpdates()
    {
      OnRequestUpdates?.Invoke();
    }

    internal event Action OnReceiveRequested;

    private bool _transmitting = false;
    private string _message = "";
    private double _progress = 0;
    private string _expiredCount = "";
    private bool _receiveEnabled = false;
    private int _objectCount = 1;
    private bool _hasOutput = false;
    private bool _autoUpdate = false;
    private CancellationTokenSource _cancellationToken;
    private Client _client;

    private Client Client
    {
      get
      {
        if (_client == null && Stream != null)
        {
          var account = Stream.GetAccount();
          _client = new Client(account);
        }

        return _client;
      }
      set { _client = value; }
    }


    //PUBLIC PROPERTIES - TO SAVE

    /// <summary>
    /// Current Stream
    /// </summary>
    public StreamWrapper Stream { get; set; }

    /// <summary>
    /// Current Branch
    /// </summary>
    public string BranchName { get; set; }

    /// <summary>
    /// Latest Commit received
    /// </summary>
    public string CommitId { get; set; }

    #region ui bindings

    /// <summary>
    /// UI Binding
    /// </summary>
    [DNJ.JsonIgnore]
    public bool Transmitting
    {
      get => _transmitting;
      set
      {
        _transmitting = value;
        RaisePropertyChanged("Transmitting");
      }
    }

    /// <summary>
    /// UI Binding
    /// </summary>
    [DNJ.JsonIgnore]
    public string Message
    {
      get => _message;
      set
      {
        _message = value;
        RaisePropertyChanged("Message");
      }
    }

    /// <summary>
    /// UI Binding
    /// </summary>
    [DNJ.JsonIgnore]
    public double Progress
    {
      get => _progress;
      set
      {
        _progress = value;
        RaisePropertyChanged("Progress");
      }
    }

    /// <summary>
    /// UI Binding
    /// </summary>
    [DNJ.JsonIgnore]
    public bool ReceiveEnabled
    {
      get => _receiveEnabled;
      set
      {
        _receiveEnabled = value;
        RaisePropertyChanged("ReceiveEnabled");
      }
    }

    //BINDINGS TO SAVE

    /// <summary>
    /// UI Binding
    /// </summary>
    public string ExpiredCount
    {
      get => _expiredCount;
      set
      {
        _expiredCount = value;
        RaisePropertyChanged("ExpiredCount");
      }
    }

    /// <summary>
    /// UI Binding
    /// </summary>
    public bool AutoUpdate
    {
      get => _autoUpdate;
      set
      {
        _autoUpdate = value;
        RaisePropertyChanged("AutoUpdate");
      }
    }

    #endregion

    /// <summary>
    /// JSON constructor, called on file open
    /// </summary>
    /// <param name="inPorts"></param>
    /// <param name="outPorts"></param>
    [DNJ.JsonConstructor]
    private Receive(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
    {
      if (inPorts.Count() == 2)
      {
        //blocker: https://github.com/DynamoDS/Dynamo/issues/11118
        //inPorts.ElementAt(1).DefaultValue = endPortDefaultValue;
      }
      else
      {
        // If information from json does not look correct, clear the default ports and add ones with default value
        InPorts.Clear();
        AddInputs();
      }

      if (!outPorts.Any())
        AddOutputs();

      ArgumentLacing = LacingStrategy.Disabled;

      this.PropertyChanged += HandlePropertyChanged;
      foreach (var port in InPorts)
      {
        port.Connectors.CollectionChanged += Connectors_CollectionChanged;
      }
    }

    /// <summary>
    /// Normal constructor, called when adding node to canvas
    /// </summary>
    public Receive()
    {
      Tracker.TrackEvent(Tracker.RECEIVE_ADDED);

      AddInputs();
      AddOutputs();

      RegisterAllPorts();
      ArgumentLacing = LacingStrategy.Disabled;

      this.PropertyChanged += HandlePropertyChanged;
      foreach (var port in InPorts)
      {
        port.Connectors.CollectionChanged += Connectors_CollectionChanged;
      }
    }


    private void AddInputs()
    {
      StringNode defaultBranchValue = new StringNode();

      defaultBranchValue.Value = "main";

      InPorts.Add(new PortModel(PortType.Input, this, new PortData("stream", "The stream to receive from")));
      InPorts.Add(new PortModel(PortType.Input, this,
        new PortData("branchName", "The branch to use to", defaultBranchValue)));
    }

    private void AddOutputs()
    {
      OutPorts.Add(new PortModel(PortType.Output, this, new PortData("data", "Data received")));
      OutPorts.Add(new PortModel(PortType.Output, this, new PortData("info", "Commit information")));
    }

    internal void CancelReceive()
    {
      _cancellationToken.Cancel();
      ResetUI();
    }

    /// <summary>
    /// Takes care of actually receiving data
    /// it then stores the data in memory and expires the node
    /// the node AST function retrieves the data from the memory and outputs it
    /// </summary>
    internal void DoReceive()
    {
      if (!InPorts[0].IsConnected)
      {
        return;
      }

      //if already receiving, stop and start again
      if (Transmitting)
        CancelReceive();

      Tracker.TrackEvent(Tracker.RECEIVE);

      Transmitting = true;
      Message = "Receiving...";

      try
      {
        if (Stream == null)
          Core.Logging.Log.CaptureAndThrow(new Exception("The stream provided is invalid"));

        void ProgressAction(ConcurrentDictionary<string, int> dict)
        {
          var val = dict.Values.Average() / _objectCount;
          Message = val.ToString("0%");
          Progress = val * 100;
        }

        void ErrorAction(string transportName, Exception exception)
        {
          Core.Logging.Log.CaptureAndThrow(exception);
        }

        void TotalChildrenCountKnown(int count) => _objectCount = count;
        _cancellationToken = new CancellationTokenSource();
        var data = Functions.Functions.Receive(Stream, BranchName, _cancellationToken.Token, ProgressAction,
          ErrorAction, TotalChildrenCountKnown);

        if (data == null) return;

        CommitId = ((Commit) data["commit"]).id;

        InMemoryCache.Set(CommitId, data);
      }
      catch (Exception e)
      {
        if (!(e.InnerException != null && e.InnerException.Message ==
          "Cannot resolve reference. The provided transport could not find it."))
          Core.Logging.Log.CaptureAndThrow(e);
      }
      finally
      {
        ResetUI();
        if (!_cancellationToken.IsCancellationRequested)
        {
          _hasOutput = true;
          ExpireNode();
        }
      }
    }


    /// <summary>
    /// Triggered when the node inputs are set for the first time or change
    /// It registers subscriptions for various events
    /// It also stores in this node a copy of the inputs (Stream and BranchName)
    /// </summary>
    /// <param name="engine"></param>
    internal void LoadInputs(EngineController engine)
    {
      if (!InPorts[0].IsConnected)
      {
        return;
      }

      var oldStream = Stream;
      //update inputs stored locally
      Stream = GetInputAs<StreamWrapper>(engine, 0);
      BranchName =
        InPorts[1].Connectors.Any()
          ? GetInputAs<string>(engine, 1)
          : "main"; //IsConnected not working because has default value

      if (Stream == null)
        return;
      if (oldStream != null && Stream.StreamId == oldStream.StreamId)
        return;

      Client?.Dispose();

      var account = Stream.GetAccount();

      Client = new Client(account);

      Client.SubscribeCommitCreated(Stream.StreamId);
      Client.SubscribeCommitDeleted(Stream.StreamId);
      Client.SubscribeCommitUpdated(Stream.StreamId);

      Client.OnCommitCreated += OnCommitChange;
      Client.OnCommitDeleted += OnCommitChange;
      Client.OnCommitUpdated += OnCommitChange;

      Task.Run(() => CheckIfBehind());
    }

    private T GetInputAs<T>(EngineController engine, int port)
    {
      var valuesNode = InPorts[port].Connectors[0].Start.Owner;
      var valuesIndex = InPorts[port].Connectors[0].Start.Index;
      var astId = valuesNode.GetAstIdentifierForOutputIndex(valuesIndex).Name;
      var inputMirror = engine.GetMirror(astId);

      if (inputMirror?.GetData() == null) return default(T);

      var data = inputMirror.GetData();

      return (T) data.Data;
    }


    private void CheckIfBehind()
    {
      if (Stream == null)
        return;
      try
      {
        var res = Client.StreamGet(Stream.StreamId).Result;
        var mainBranch = res.branches.items.FirstOrDefault(b => b.name == BranchName);
        if (!mainBranch.commits.items.Any())
          return;

        var lastCommit = mainBranch.commits.items[0];

        //it's behind! Get objects count from last commit
        if (lastCommit.id != CommitId)
        {
          GetExpiredObjectCount(lastCommit.referencedObject);
        }
      }
      catch (Exception e)
      {
        Core.Logging.Log.CaptureException(e);
      }
    }

    private void GetExpiredObjectCount(string objectId)
    {
      if (Stream == null)
        return;
      try
      {
        var @object = Client.ObjectGet(Stream.StreamId, objectId).Result;
        ExpiredCount = @object.totalChildrenCount.ToString();
        _objectCount = @object.totalChildrenCount;
      }
      catch (Exception e)
      {
        Core.Logging.Log.CaptureException(e);
      }
    }

    private void ExpireNode()
    {
      OnNodeModified(true);
    }

    private void ResetUI()
    {
      Transmitting = false;
      ExpiredCount = "";
      Message = "";
      Progress = 0;
      _hasOutput = false;
    }

    #region events

    private void OnCommitChange(object sender, CommitInfo e)
    {
      Task.Run(() => GetExpiredObjectCount(e.objectId));
      if (AutoUpdate)
        OnReceiveRequested?.Invoke();
    }

    void Connectors_CollectionChanged(object sender,
      System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
      RequestUpdates();
    }

    void HandlePropertyChanged(object sender, PropertyChangedEventArgs e)
    {
      if (e.PropertyName != "CachedValue")
        return;

      if (InPorts[0].Connectors.Count == 0)
        return;

      RequestUpdates();
    }

    #endregion

    #region overrides

    /// <summary>
    /// 
    /// </summary>
    /// <param name="inputAstNodes"></param>
    /// <returns></returns>
    public override IEnumerable<AssociativeNode> BuildOutputAst(List<AssociativeNode> inputAstNodes)
    {
      if (!InPorts[0].IsConnected)
      {
        ReceiveEnabled = false;
        ExpiredCount = "";
        return OutPorts.Enumerate().Select(output =>
          AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(output.Index), new NullNode()));
      }

      if (!_hasOutput)
      {
        ReceiveEnabled = true;
      }
      else
      {
        _hasOutput = false;
        var associativeNodes = new List<AssociativeNode>();
        var primitiveNode = AstFactory.BuildStringNode(CommitId);
        var dataFunctionCall = AstFactory.BuildFunctionCall(
          new Func<string, object>(Functions.Functions.ReceiveData),
          new List<AssociativeNode> {primitiveNode});

        var infoFunctionCall = AstFactory.BuildFunctionCall(
          new Func<string, string>(Functions.Functions.ReceiveInfo),
          new List<AssociativeNode> {primitiveNode});

        associativeNodes.Add(AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), dataFunctionCall));
        associativeNodes.Add(AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(1), infoFunctionCall));
        return associativeNodes;
      }

      return OutPorts.Enumerate().Select(output =>
        AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(output.Index), new NullNode()));
    }

    /// <summary>
    /// 
    /// </summary>
    public override void Dispose()
    {
      Client?.Dispose();
      base.Dispose();
    }

    #endregion
  }
}
