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
using System.Windows;
using Dynamo.Engine;
using Dynamo.Utilities;

namespace Speckle.ConnectorDynamo.SendNode
{
  [NodeName("Send")]
  [NodeCategory("Speckle 2")]
  [NodeDescription("Send data to a Speckle server")]
  [NodeSearchTags("send", "speckle")]

  [IsDesignScriptCompatible]
  public class Send : NodeModel
  {
    private bool _transmitting = false;
    private string _message = "";
    private bool _expired = false;
    private bool _sendEnabled = false;

    [DNJ.JsonIgnore]
    public bool Transmitting { get => _transmitting; set { _transmitting = value; RaisePropertyChanged("Transmitting"); } }

    [DNJ.JsonIgnore]
    public string Message { get => _message; set { _message = value; RaisePropertyChanged("Message"); } }
    [DNJ.JsonIgnore]
    public bool Expired { get => _expired; set { _expired = value; RaisePropertyChanged("Expired"); } }
    [DNJ.JsonIgnore]
    public bool SendEnabled { get => _sendEnabled; set { _sendEnabled = value; RaisePropertyChanged("SendEnabled"); } }

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
      //OutPorts.Add(new PortModel(PortType.Output, this, new PortData("log", "Log")));
    }

    internal void DoSend(EngineController engine)
    {
      if (!InPorts[0].IsConnected || !InPorts[1].IsConnected)
      {
        return;
      }

      Tracker.TrackEvent(Tracker.SEND);

      Transmitting = true;
      // _sendClicked = true;
      Message = "Sending...";
      //ExpireNode();

      try
      {
        var data = GetInputAs<object>(engine, 0);
        var stream = GetInputAs<StreamWrapper>(engine, 1);

        Functions.Functions.Send(data, stream);
      }
      catch (Exception)
      {

      }

      Transmitting = false;
      Expired = false;
      Message = "";

    }

    private T GetInputAs<T>(EngineController engine, int port)
    {
      var valuesNode = InPorts[port].Connectors[0].Start.Owner;
      var valuesIndex = InPorts[port].Connectors[0].Start.Index;
      var astId = valuesNode.GetAstIdentifierForOutputIndex(valuesIndex).Name;
      var inputMirror = engine.GetMirror(astId);

      if (inputMirror == null || inputMirror.GetData() == null) return default(T);

      var data = inputMirror.GetData();
      if (data.IsCollection)
      {
        var elements = data.GetElements();
        foreach (var element in elements)
        {
          if (element.IsCollection)
          {
            var elements2 = element.GetElements();
            foreach (var element2 in elements2)
            {
              if (element2.IsCollection)
              {
                var elements3 = element2.GetElements();
                foreach (var element3 in elements3)
                {
                  if (element3.IsCollection)
                  {
                    var elements4 = element3.GetElements();
                  }

                }
              }

            }
          }
        }
        return (T)elements.FirstOrDefault().Data;

      }
      else
      {
        return (T)data.Data;
      }
    }

    public void ExpireNode()
    {
      OnNodeModified(true);
    }

    #region overrides
    /// <summary>
    /// Sending is actually only happening when clicking the button on the node UI, this function is only used to check what ports are connected
    /// could be done differently, but this is an easy way
    /// </summary>
    /// <param name="inputAstNodes"></param>
    /// <returns></returns>
    public override IEnumerable<AssociativeNode> BuildOutputAst(List<AssociativeNode> inputAstNodes)
    {
      if (!InPorts[0].IsConnected || !InPorts[1].IsConnected)
      {
        SendEnabled = false;
        return new[] { AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), AstFactory.BuildNullNode()) };
      }

      SendEnabled = true;
      Expired = true;


      return OutPorts.Enumerate().Select(output => AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(output.Index), new NullNode()));
    }

    public override void Dispose()
    {
      base.Dispose();
    }

    #endregion



  }


}
