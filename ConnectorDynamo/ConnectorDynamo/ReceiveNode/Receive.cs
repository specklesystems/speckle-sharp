extern alias DynamoNewtonsoft;
using DNJ = DynamoNewtonsoft::Newtonsoft.Json;

using Dynamo.Engine;
using Dynamo.Graph.Nodes;
using ProtoCore.AST.AssociativeAST;
using ProtoCore.Mirror;
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

namespace Speckle.ConnectorDynamo.ReceiveNode
{
  [NodeName("Receive")]
  [NodeCategory("Speckle 2")]
  [NodeDescription("Receive data from a Speckle server")]
  [NodeSearchTags("receive", "speckle")]

  [IsDesignScriptCompatible]
  public class Receive : NodeModel
  {

    public event Action RequestChangeStream;
    protected virtual void OnRequestChangeStreamId()
    {
      RequestChangeStream?.Invoke();
    }

    public event Action RequestReceive;
    //protected virtual void OnRequestReceive()
    //{
    //  RequestReceive?.Invoke();
    //}

    private bool _transmitting = false;
    private string _message = "";
    private double _progress = 0;
    private string _expiredCount = "";
    private bool _receiveEnabled = false;
    private int _objectCount = 1;
    private bool _hasOutput = false;
    private bool _autoUpdate = true;
    private string _dataId = "";
    public CancellationTokenSource _cancellationToken;

    //ignored properties
    //NOT to be saved with the file
    [DNJ.JsonIgnore]
    public bool Transmitting { get => _transmitting; set { _transmitting = value; RaisePropertyChanged("Transmitting"); } }

    [DNJ.JsonIgnore]
    public string Message { get => _message; set { _message = value; RaisePropertyChanged("Message"); } }
    [DNJ.JsonIgnore]
    public double Progress { get => _progress; set { _progress = value; RaisePropertyChanged("Progress"); } }
    [DNJ.JsonIgnore]
    public bool ReceiveEnabled { get => _receiveEnabled; set { _receiveEnabled = value; RaisePropertyChanged("ReceiveEnabled"); } }

    //properties TO SAVE
    public string ExpiredCount { get => _expiredCount; set { _expiredCount = value; RaisePropertyChanged("ExpiredCount"); } }
    public bool AutoUpdate { get => _autoUpdate; set { _autoUpdate = value; RaisePropertyChanged("AutoUpdate"); } }


    private string _oldStreamId { get; set; }

    private Client _client { get; set; }

    private void AddInputs()
    {
      StringNode defaultBranchValue = new StringNode();

      defaultBranchValue.Value = "main";

      InPorts.Add(new PortModel(PortType.Input, this, new PortData("stream", "The stream to receive from")));
      InPorts.Add(new PortModel(PortType.Input, this, new PortData("branchName", "The branch to use to", defaultBranchValue)));
    }
    private void AddOutputs()
    {
      OutPorts.Add(new PortModel(PortType.Output, this, new PortData("data", "Data received")));
      OutPorts.Add(new PortModel(PortType.Output, this, new PortData("info", "Commit information")));
    }


    [DNJ.JsonConstructor]
    private Receive(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
    {

      if (inPorts.Count() == 1)
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

      if (outPorts.Count() == 0)
        AddOutputs();

      ArgumentLacing = LacingStrategy.Disabled;

      this.PropertyChanged += HandlePropertyChanged;
      foreach (var port in InPorts)
      {
        port.Connectors.CollectionChanged += Connectors_CollectionChanged;
      }
    }

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

    void Connectors_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
      OnRequestChangeStreamId();
    }

    void HandlePropertyChanged(object sender, PropertyChangedEventArgs e)
    {
      if (e.PropertyName != "CachedValue")
        return;

      if (InPorts[0].Connectors.Count == 0)
        return;

      OnRequestChangeStreamId();
    }

    internal void CancelReceive()
    {
      _cancellationToken.Cancel();
      Transmitting = false;
      ExpiredCount = "";
      Message = "";
      Progress = 0;
      _hasOutput = false;
    }


    internal void DoReceive(EngineController engine)
    {
      if (!InPorts[0].IsConnected)
      {
        return;
      }

      Tracker.TrackEvent(Tracker.RECEIVE);

      Transmitting = true;
      Message = "Receiving...";

      try
      {
        var stream = GetInputAs<StreamWrapper>(engine, 0);
        var branchName = InPorts[1].Connectors.Any() ? GetInputAs<string>(engine, 1) : ""; //IsConnected not working because has default value

        if (stream == null)
          Core.Logging.Log.CaptureAndThrow(new Exception("The stream provided is invalid"));

        Action<ConcurrentDictionary<string, int>> onProgressAction = (dict) =>
        {
          var val = (double)dict.Values.Last() / _objectCount;
          Message = val.ToString("0%");
          Progress = val * 100;
        };

        Action<string, Exception> onErroAction = (transportName, exception) =>
        {
          Core.Logging.Log.CaptureAndThrow(exception);
        };

        Action<int> onTotalChildrenCountKnown = (count) => _objectCount = count;
        _cancellationToken = new CancellationTokenSource();
        var data = Functions.Functions.Receive(stream, branchName, _cancellationToken.Token, onProgressAction, onErroAction, onTotalChildrenCountKnown);
        _dataId = ((Commit)data["commit"]).id;
        InMemoryCache.Set(_dataId, data);

      }
      catch (Exception e)
      {
        Core.Logging.Log.CaptureAndThrow(e);
      }
      finally
      {
        Transmitting = false;
        ExpiredCount = "";
        Message = "";
        Progress = 0;
        _hasOutput = true;
        ExpireNode();
      }

    }


    private T GetInputAs<T>(EngineController engine, int port)
    {
      var valuesNode = InPorts[port].Connectors[0].Start.Owner;
      var valuesIndex = InPorts[port].Connectors[0].Start.Index;
      var astId = valuesNode.GetAstIdentifierForOutputIndex(valuesIndex).Name;
      var inputMirror = engine.GetMirror(astId);

      if (inputMirror == null || inputMirror.GetData() == null) return default(T);

      var data = inputMirror.GetData();

      return (T)data.Data;
    }

    /// <summary>
    /// Triggered when the Stream input is set for the first time or changes
    /// It registers subscriptions for various events
    /// </summary>
    /// <param name="engine"></param>
    internal void ChangeStreams(EngineController engine)
    {
      if (!InPorts[0].IsConnected)
      {
        return;
      }

      var valuesNode = InPorts[0].Connectors[0].Start.Owner;
      var valuesIndex = InPorts[0].Connectors[0].Start.Index;
      var astId = valuesNode.GetAstIdentifierForOutputIndex(valuesIndex).Name;
      var stream = GetInputAsStream(engine.GetMirror(astId));

      if (stream == null)
        return;
      else if (stream == null && _client != null)
        _client.Dispose();
      else if (stream.StreamId == _oldStreamId)
        return;
      else
      {
        if (_client != null)
          _client.Dispose();

        _oldStreamId = stream.StreamId;
        var account = stream.GetAccount();

        _client = new Client(account);

        _client.SubscribeCommitCreated(stream.StreamId);
        _client.SubscribeCommitDeleted(stream.StreamId);
        _client.SubscribeCommitUpdated(stream.StreamId);

        _client.OnCommitCreated += OnCommitChange;
        _client.OnCommitDeleted += OnCommitChange;
        _client.OnCommitUpdated += OnCommitChange;
      }
    }

    private static StreamWrapper GetInputAsStream(RuntimeMirror inputMirror)
    {
      StreamWrapper input = null;

      if (inputMirror == null || inputMirror.GetData() == null) return input;

      var data = inputMirror.GetData();
      if (data.IsCollection)
      {
        var elements = data.GetElements().Select(e => e.Data);
        return elements.FirstOrDefault() as StreamWrapper;
      }
      else
      {
        return data.Data as StreamWrapper;
      }
    }

    private void OnCommitChange(object sender, CommitInfo e)
    {
      ExpiredCount = "⬤";
      if (AutoUpdate)
        RequestReceive?.Invoke();
    }

    public void ExpireNode()
    {
      OnNodeModified(true);
    }

    #region overrides
    public override IEnumerable<AssociativeNode> BuildOutputAst(List<AssociativeNode> inputAstNodes)
    {
      if (!InPorts[0].IsConnected)
      {
        ReceiveEnabled = false;
        ExpiredCount = "";
        return OutPorts.Enumerate().Select(output => AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(output.Index), new NullNode()));
      }

      ReceiveEnabled = true;
      ExpiredCount = "⬤";

      if (_hasOutput)
      {
        ExpiredCount = "";
        var associativeNodes = new List<AssociativeNode>();
        var primitiveNode = AstFactory.BuildStringNode(_dataId);
        var datafunctionCall = AstFactory.BuildFunctionCall(
          new Func<string, object>(Functions.Functions.ReceiveData),
          new List<AssociativeNode> { primitiveNode });

        var infofunctionCall = AstFactory.BuildFunctionCall(
          new Func<string, string>(Functions.Functions.ReceiveInfo),
          new List<AssociativeNode> { primitiveNode });

        associativeNodes.Add(AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), datafunctionCall));
        associativeNodes.Add(AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(1), infofunctionCall));
        return associativeNodes;
      }

      return OutPorts.Enumerate().Select(output => AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(output.Index), new NullNode()));
    }

    public override void Dispose()
    {
      if (_client != null)
        _client.Dispose();

      base.Dispose();
    }

    #endregion

  }


}
