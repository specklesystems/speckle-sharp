extern alias DynamoNewtonsoft;
using DNJ = DynamoNewtonsoft::Newtonsoft.Json;

using Dynamo.Engine;
using Dynamo.Graph.Nodes;
using Newtonsoft.Json;
using ProtoCore.AST.AssociativeAST;
using ProtoCore.Mirror;
using Speckle.ConnectorDynamo.Functions;
using Speckle.Core.Api;
using Speckle.Core.Api.SubscriptionModels;
using Speckle.Core.Credentials;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Speckle.Core.Logging;
using Speckle.ConnectorDynamo.Functions.Extras;

namespace Speckle.ConnectorDynamo
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
    private readonly NullNode defaultAccountValue = new NullNode();

    public string OldStreamId { get; set; }
    public StreamWrapper Stream { get; set; }

    private Client _client { get; set; }

    private void AddInputs()
    {
      InPorts.Add(new PortModel(PortType.Input, this, new PortData("stream", "The stream to receive from")));
    }
    private void AddOutputs()
    {
      OutPorts.Add(new PortModel(PortType.Output, this, new PortData("data", "Data received")));
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

      this.PropertyChanged += StreamId_PropertyChanged;
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

      this.PropertyChanged += StreamId_PropertyChanged;
      foreach (var port in InPorts)
      {
        port.Connectors.CollectionChanged += Connectors_CollectionChanged;
      }

    }

    void Connectors_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
      OnRequestChangeStreamId();
    }

    void StreamId_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
      if (e.PropertyName != "CachedValue")
        return;

      if (InPorts.Any(x => x.Connectors.Count == 0))
        return;

      OnRequestChangeStreamId();
    }

    #region overrides
    public override IEnumerable<AssociativeNode> BuildOutputAst(List<AssociativeNode> inputAstNodes)
    {
      if (!InPorts[0].IsConnected)
      {
        return new[] { AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), AstFactory.BuildNullNode()) };
      }

      Tracker.TrackEvent(Tracker.RECEIVE);

      var functionCall = AstFactory.BuildFunctionCall(
        new Func<StreamWrapper, object>(Functions.Functions.Receive),
        new List<AssociativeNode> { inputAstNodes[0] });

      return new[] { AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), functionCall) };
    }

    public override void Dispose()
    {
      if (_client != null)
        _client.Dispose();

      base.Dispose();
    }

    #endregion

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
      Stream = GetInputAsStream(engine.GetMirror(astId));
      
      if (Stream == null)
        return;
      else if (Stream == null && _client != null)
        _client.Dispose();
      else if (Stream.StreamId == OldStreamId)
        return;
      else
      {
        var account = Stream.GetAccount();

        _client = new Client(account);

        _client.SubscribeCommitCreated(Stream.StreamId);
        _client.SubscribeCommitDeleted(Stream.StreamId);
        _client.SubscribeCommitUpdated(Stream.StreamId);

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
        //foreach (var element in elements)
        //}
      }
      else
      {
        return data.Data as StreamWrapper;
      }
    }

    private void OnCommitChange(object sender, CommitInfo e)
    {
      OnNodeModified(forceExecute: true);
    }

  }


}
