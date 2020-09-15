using Dynamo.Graph.Nodes;
using Microsoft.Practices.Prism.Regions;
using Newtonsoft.Json;
using ProtoCore.AST.AssociativeAST;
using Speckle.ConnectorDynamo.Functions;
using Speckle.Core.Api;
using Speckle.Core.Api.SubscriptionModels;
using Speckle.Core.Credentials;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Speckle.ConnectorDynamo
{
  [NodeName("Receive2")]
  [NodeCategory("Speckle")]
  [NodeDescription("Receives data from Speckle")]
  [NodeSearchTags("receive", "speckle")]

  //Inputs
  [InPortNames("StreamId", "Account")]
  [InPortTypes("string", "Speckle.Core.Credentials.Account")]
  [InPortDescriptions("StreamID", "Account (optional)")]

  [OutPortNames("Data")]
  [OutPortTypes("object")]
  [OutPortDescriptions("Data received")]

  [IsDesignScriptCompatible]
  public class Receive : NodeModel
  {
    public string OldStreamId { get; set; }
    public string StreamId { get; set; }


    private Client _client { get; set; }
    private bool _shouldUpdate = false;

    [JsonConstructor]
    private Receive(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
    {

    }
    public Receive()
    {
      RegisterAllPorts();

    }

    #region overrides
    public override IEnumerable<AssociativeNode> BuildOutputAst(List<AssociativeNode> inputAstNodes)
    {
      if (!InPorts[0].IsConnected)
      {
        return new[] { AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), AstFactory.BuildNullNode()) };
      }
      if (!_shouldUpdate)
      {
        return new[] { AstFactory.BuildAssignment(
                          AstFactory.BuildIdentifier(AstIdentifierBase + "_dummy"),
                          VMDataBridge.DataBridge.GenerateBridgeDataAst(GUID.ToString(), inputAstNodes[0])) };
      }
      _shouldUpdate = false;

      var functionCall = AstFactory.BuildFunctionCall(
        new Func<string, Account, object>(Functions.Speckle.Receive),
        new List<AssociativeNode> { inputAstNodes[0], inputAstNodes[1] });

      return new[] { AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), functionCall) };
    }

    protected override void OnBuilt()
    {
      base.OnBuilt();
      VMDataBridge.DataBridge.Instance.RegisterCallback(GUID.ToString(), DataBridgeCallback);
    }

    public override void Dispose()
    {
      if (_client != null)
        _client.Dispose();

      base.Dispose();
      VMDataBridge.DataBridge.Instance.UnregisterCallback(GUID.ToString());
    }

    #endregion

    private void DataBridgeCallback(object obj)
    {
      try
      {
        //ID disconnected
        if (!InPorts[0].Connectors.Any())
        {
          StreamId = "";
          ChangeStreams();
          return;
        }
        //ID connected
        else if (InPorts[0].Connectors.Any() && obj != null)
        {
          StreamId = (string)obj;
          if (StreamId != OldStreamId)
          {
            ChangeStreams();
          }
        }
      }
      catch (Exception ex)
      {
        Warning("Inputs are not formatted correctly");
      }
    }

    private void ChangeStreams()
    {
      if (string.IsNullOrEmpty(StreamId) && _client != null)
        _client.Dispose();
      else
      {
        var account = AccountManager.GetDefaultAccount();

        _client = new Client(account);

        _client.SubscribeCommitCreated(StreamId);
        _client.SubscribeCommitDeleted(StreamId);
        _client.SubscribeCommitUpdated(StreamId);

        _client.OnCommitCreated += OnCommitChange;
        _client.OnCommitDeleted += OnCommitChange;
        _client.OnCommitUpdated += OnCommitChange;
      }
    }

    private void OnCommitChange(object sender, CommitInfo e)
    {
      _shouldUpdate = true;
      OnNodeModified(forceExecute: true);
    }

  }


}
