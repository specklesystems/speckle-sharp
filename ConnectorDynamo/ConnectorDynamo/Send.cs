extern alias DynamoNewtonsoft;
using DNJ = DynamoNewtonsoft::Newtonsoft.Json;

using Dynamo.Graph.Nodes;
using Newtonsoft.Json;
using ProtoCore.AST.AssociativeAST;
using Speckle.ConnectorDynamo.Functions;
using Speckle.Core.Credentials;
using System;
using System.Collections.Generic;
using System.Linq;
using Account = Speckle.Core.Credentials.Account;
using Speckle.Core.Logging;
using Speckle.ConnectorDynamo.Functions.Extras;

namespace Speckle.ConnectorDynamo
{
  [NodeName("Send")]
  [NodeCategory("Speckle 2")]
  [NodeDescription("Send data to a Speckle server")]
  [NodeSearchTags("send", "speckle")]

  [IsDesignScriptCompatible]
  public class Send : NodeModel
  {


    private void AddInputs()
    {
      StringNode defaultBranchValue = new StringNode();
      StringNode defaultMessageValue = new StringNode();

      defaultBranchValue.Value = "main";
      defaultMessageValue.Value = "Automatic commit from Dynamo";

      InPorts.Add(new PortModel(PortType.Input, this, new PortData("data", "The data to send")));
      InPorts.Add(new PortModel(PortType.Input, this, new PortData("stream", "The stream to send to")));
      InPorts.Add(new PortModel(PortType.Input, this, new PortData("branchName", "The branch to use to", defaultBranchValue)));
      InPorts.Add(new PortModel(PortType.Input, this, new PortData("message", "The commit message", defaultMessageValue)));
    }
    private void AddOutputs()
    {
      OutPorts.Add(new PortModel(PortType.Output, this, new PortData("log", "Log")));
    }


    [DNJ.JsonConstructor]
    private Send(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
    {
      if (inPorts.Count() == 4)
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
    }

    public Send()
    {
      Tracker.TrackEvent(Tracker.SEND_ADDED);

      AddInputs();
      AddOutputs();

      RegisterAllPorts();
      ArgumentLacing = LacingStrategy.Disabled;
    }


    #region overrides
    public override IEnumerable<AssociativeNode> BuildOutputAst(List<AssociativeNode> inputAstNodes)
    {
      if (!InPorts[0].IsConnected || !InPorts[1].IsConnected)
      {
        return new[] { AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), AstFactory.BuildNullNode()) };
      }

      Tracker.TrackEvent(Tracker.SEND);

      var functionCall = AstFactory.BuildFunctionCall(
        new Func<object, StreamWrapper, string, string, string>(Functions.Functions.Send),
        new List<AssociativeNode> { inputAstNodes[0], inputAstNodes[1], inputAstNodes[2], inputAstNodes[3] });

      return new[] { AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), functionCall) };
    }

    public override void Dispose()
    {
      base.Dispose();
    }

    #endregion



  }


}
