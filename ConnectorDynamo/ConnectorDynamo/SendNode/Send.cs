extern alias DynamoNewtonsoft;
using DNJ = DynamoNewtonsoft::Newtonsoft.Json;

using Dynamo.Graph.Nodes;
using ProtoCore.AST.AssociativeAST;
using Speckle.ConnectorDynamo.Functions;
using Speckle.Core.Credentials;
using System;
using System.Collections.Generic;
using System.Linq;
using Speckle.Core.Logging;
using Dynamo.Engine;
using Dynamo.Utilities;
using ProtoCore.Mirror;
using System.Collections.Concurrent;
using System.ComponentModel;

namespace Speckle.ConnectorDynamo.SendNode
{
  [NodeName("Send")]
  [NodeCategory("Speckle 2")]
  [NodeDescription("Send data to a Speckle server")]
  [NodeSearchTags("send", "speckle")]

  [IsDesignScriptCompatible]
  public class Send : NodeModel
  {
    //detect changes (eg connectors plugged/unplugged)
    public event Action RequestChanges;
    protected virtual void OnRequestChanges()
    {
      RequestChanges?.Invoke();
    }

    private bool _transmitting = false;
    private string _message = "";
    private double _progress = 0;
    private string _expiredCount = "";
    private bool _sendEnabled = false;
    private int _objectCount = 0;
    private bool _hasOutput = false;
    private string _outputInfo = "";

    //ignored properties
    //NOT to be saved with the file
    [DNJ.JsonIgnore]
    public bool Transmitting { get => _transmitting; set { _transmitting = value; RaisePropertyChanged("Transmitting"); } }

    [DNJ.JsonIgnore]
    public string Message { get => _message; set { _message = value; RaisePropertyChanged("Message"); } }
    [DNJ.JsonIgnore]
    public double Progress { get => _progress; set { _progress = value; RaisePropertyChanged("Progress"); } }
    [DNJ.JsonIgnore]
    public bool SendEnabled { get => _sendEnabled; set { _sendEnabled = value; RaisePropertyChanged("SendEnabled"); } }

    //properties TO SAVE
    public string ExpiredCount { get => _expiredCount; set { _expiredCount = value; RaisePropertyChanged("ExpiredCount"); } }

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

      this.PropertyChanged += HandlePropertyChanged;
    }

    public Send()
    {
      Tracker.TrackEvent(Tracker.SEND_ADDED);

      AddInputs();
      AddOutputs();

      RegisterAllPorts();
      ArgumentLacing = LacingStrategy.Disabled;

      this.PropertyChanged += HandlePropertyChanged;
    }

    void HandlePropertyChanged(object sender, PropertyChangedEventArgs e)
    {
      if (e.PropertyName != "CachedValue")
        return;

      if ((!InPorts[0].IsConnected || !InPorts[1].IsConnected))
        return;

      //the node was expired manually just to output the commit info, no need to do anything!
      if (_hasOutput)
      {
        _hasOutput = false;
        return;
      }

      OnRequestChanges();
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
      OutPorts.Add(new PortModel(PortType.Output, this, new PortData("info", "Commit information")));
    }

    internal void DoSend(EngineController engine)
    {
      if (!InPorts[0].IsConnected || !InPorts[1].IsConnected)
      {
        return;
      }

      Tracker.TrackEvent(Tracker.SEND);

      Transmitting = true;
      Message = "Converting...";

      try
      {
        var data = GetInputAs<object>(engine, 0);
        var stream = GetInputAs<StreamWrapper>(engine, 1);
        var branchName = InPorts[2].Connectors.Any() ? GetInputAs<string>(engine, 2) : ""; //IsConnected not working because has default value
        var message = InPorts[3].Connectors.Any() ? GetInputAs<string>(engine, 3) : ""; //IsConnected not working because has default value

        if (stream == null)
          Core.Logging.Log.CaptureAndThrow(new Exception("The stream provided is invalid"));

        var converter = new BatchConverter();
        var @base = converter.ConvertRecursivelyToSpeckle(data);
        var totalCount = @base.GetTotalChildrenCount();
        Message = "Sending...";
        Action<ConcurrentDictionary<string, int>> onProgressAction = (dict) =>
        {

          var val = (double)dict.Values.Last() / totalCount;
          Message = val.ToString("0%");
          Progress = val * 100;

        };

        Action<string, Exception> onErroAction = (transportName, exception) =>
        {
          Core.Logging.Log.CaptureAndThrow(exception);
        };



        var plural = (totalCount == 1) ? "" : "s";
        message = string.IsNullOrEmpty(message) ? $"Sent {totalCount} object{plural} from Dynamo" : message;

        var commitId = Functions.Functions.Send(@base, stream, branchName, message, onProgressAction, onErroAction);
        stream.CommitId = commitId;
        _outputInfo = stream.ToString();
      }
      catch (Exception e)
      {
        _outputInfo = e.Message;
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


    internal void UpdateNodeUi(EngineController engine)
    {
      if (!InPorts[0].IsConnected || !InPorts[1].IsConnected)
      {
        ExpiredCount = "";
        return;
      }

      _objectCount = 0;
      var data = GetInputAs<object>(engine, 0, true);

      ExpiredCount = _objectCount.ToString();
    }


    private T GetInputAs<T>(EngineController engine, int port, bool count = false)
    {
      var valuesNode = InPorts[port].Connectors[0].Start.Owner;
      var valuesIndex = InPorts[port].Connectors[0].Start.Index;
      var astId = valuesNode.GetAstIdentifierForOutputIndex(valuesIndex).Name;
      var inputMirror = engine.GetMirror(astId);



      if (inputMirror == null || inputMirror.GetData() == null) return default(T);

      var data = inputMirror.GetData();
      var value = RecurseInput(data, count);

      return (T)value;
    }

    private object RecurseInput(MirrorData data, bool count)
    {
      object @object;
      if (data.IsCollection)
      {
        var list = new List<object>();
        var elements = data.GetElements();
        list.AddRange(elements.Select(x => RecurseInput(x, count)));
        @object = list;
      }
      else
      {
        @object = data.Data;
        if (count)
          _objectCount++;
      }
      return @object;
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
        ExpiredCount = "";
        _outputInfo = "";
      }

      SendEnabled = true;

      return new[] { AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), AstFactory.BuildStringNode(_outputInfo)) };

      //return OutPorts.Enumerate().Select(output => AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(output.Index), new NullNode()));
    }

    public override void Dispose()
    {
      base.Dispose();
    }

    #endregion



  }


}
