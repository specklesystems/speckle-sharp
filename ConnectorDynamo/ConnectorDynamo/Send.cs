using Dynamo.Graph.Nodes;
using Newtonsoft.Json;
using ProtoCore.AST.AssociativeAST;
using Speckle.Core.Credentials;
using System;
using System.Collections.Generic;
using System.Linq;


namespace Speckle.ConnectorDynamo
{
  [NodeName("Send")]
  [NodeCategory("Speckle 2")]
  [NodeDescription("Send data to a Speckle server")]
  [NodeSearchTags("send", "speckle")]

  [IsDesignScriptCompatible]
  public class Send : NodeModel
  {
    private readonly NullNode defaultAccountValue = new NullNode();


    [JsonConstructor]
    private Send(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
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
        InPorts.Add(new PortModel(PortType.Input, this, new PortData("data", "The data to send")));
        InPorts.Add(new PortModel(PortType.Input, this, new PortData("streamId", "The stream to send to")));
        InPorts.Add(new PortModel(PortType.Input, this, new PortData("account", "Speckle account to use, if not provided the default account will be used", defaultAccountValue)));
      }

      if (outPorts.Count() == 0)
        OutPorts.Add(new PortModel(PortType.Output, this, new PortData("log", "Log")));

    }

    public Send()
    {
      InPorts.Add(new PortModel(PortType.Input, this, new PortData("data", "The data to send")));
      InPorts.Add(new PortModel(PortType.Input, this, new PortData("streamId", "The stream to send to")));
      InPorts.Add(new PortModel(PortType.Input, this, new PortData("account", "Speckle account to use, if not provided the default account will be used", defaultAccountValue)));

      OutPorts.Add(new PortModel(PortType.Output, this, new PortData("log", "Log")));

      RegisterAllPorts();
    }


    #region overrides
    public override IEnumerable<AssociativeNode> BuildOutputAst(List<AssociativeNode> inputAstNodes)
    {
      if (!InPorts[0].IsConnected || !InPorts[1].IsConnected)
      {
        return new[] { AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), AstFactory.BuildNullNode()) };
      }

      var functionCall = AstFactory.BuildFunctionCall(
        new Func<object, string, Account, object>(Functions.Functions.Send),
        new List<AssociativeNode> { inputAstNodes[0], inputAstNodes[1], inputAstNodes[2] });

      return new[] { AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), functionCall) };
    }

    public override void Dispose()
    {
      base.Dispose();
    }

    #endregion



  }


}
