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
using ProtoCore.Mirror;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Threading;
using Speckle.Core.Api;
using Speckle.Core.Models;

namespace Speckle.ConnectorDynamo.SendNode
{
  /// <summary>
  /// Send data to Speckle
  /// </summary>
  [NodeName("Send")]
  [NodeCategory("Speckle 2")]
  [NodeDescription("Send data to a Speckle server")]
  [NodeSearchTags("send", "speckle")]
  [IsDesignScriptCompatible]
  public class Send : NodeModel
  {
    #region private fields

    private bool _transmitting = false;
    private string _message = "";
    private double _progress = 0;
    private string _expiredCount = "";
    private bool _sendEnabled = false;
    private int _objectCount = 0;
    private bool _hasOutput = false;
    private string _outputInfo = "";
    private bool _firstRun = true;
    private CancellationTokenSource _cancellationToken;

    #endregion

    #region ui bindings

    //PUBLIC PROPERTIES
    //NOT to be saved with the file

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
    public bool SendEnabled
    {
      get => _sendEnabled;
      set
      {
        _sendEnabled = value;
        RaisePropertyChanged("SendEnabled");
      }
    }

    //properties TO SAVE
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

    #endregion


    /// <summary>
    /// JSON constructor, called on file open
    /// </summary>
    /// <param name="inPorts"></param>
    /// <param name="outPorts"></param>
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

    /// <summary>
    /// Normal constructor, called when adding node to canvas
    /// </summary>
    public Send()
    {
      Tracker.TrackEvent(Tracker.SEND_ADDED);

      AddInputs();
      AddOutputs();

      RegisterAllPorts();
      ArgumentLacing = LacingStrategy.Disabled;
      this.PropertyChanged += HandlePropertyChanged;
    }

    private void AddInputs()
    {
      StringNode defaultBranchValue = new StringNode();
      StringNode defaultMessageValue = new StringNode();

      defaultBranchValue.Value = "main";
      defaultMessageValue.Value = "Automatic commit from Dynamo";

      InPorts.Add(new PortModel(PortType.Input, this, new PortData("data", "The data to send")));
      InPorts.Add(new PortModel(PortType.Input, this, new PortData("stream", "The stream to send to")));
      InPorts.Add(new PortModel(PortType.Input, this,
        new PortData("branchName", "The branch to use to", defaultBranchValue)));
      InPorts.Add(new PortModel(PortType.Input, this,
        new PortData("message", "The commit message", defaultMessageValue)));
    }

    private void AddOutputs()
    {
      OutPorts.Add(new PortModel(PortType.Output, this, new PortData("info", "Commit information")));
    }


    internal void CancelSend()
    {
      _cancellationToken.Cancel();
      ResetUI();
    }

    /// <summary>
    /// Takes care of actually sending the data
    /// </summary>
    /// <param name="engine"></param>
    internal void DoSend(EngineController engine)
    {
      if (!InPorts[0].IsConnected || !InPorts[1].IsConnected)
      {
        return;
      }

      //if already receiving, stop and start again
      if (Transmitting)
        CancelSend();

      Tracker.TrackEvent(Tracker.SEND);

      Transmitting = true;
      Message = "Converting...";

      try
      {
        //get input values via data mirror
        var data = GetInputAs<object>(engine, 0);
        var inputStream = GetInputAs<StreamWrapper>(engine, 1);
        var branchName =
          InPorts[2].Connectors.Any()
            ? GetInputAs<string>(engine, 2)
            : ""; //IsConnected not working because has default value
        var message =
          InPorts[3].Connectors.Any()
            ? GetInputAs<string>(engine, 3)
            : ""; //IsConnected not working because has default value

        if (inputStream == null)
          Core.Logging.Log.CaptureAndThrow(new Exception("The stream provided is invalid"));
        
        //avoid editing upstream stream!
        var stream = new StreamWrapper(inputStream.StreamId,inputStream.AccountId, inputStream.ServerUrl);

        var converter = new BatchConverter();
        var @base = converter.ConvertRecursivelyToSpeckle(data);
        var totalCount = @base.GetTotalChildrenCount();
        Message = "Sending...";

        void ProgressAction(ConcurrentDictionary<string, int> dict)
        {
          var val = (double) dict.Values.Average() / totalCount;
          Message = val.ToString("0%");
          Progress = val * 100;
        }

        void ErrorAction(string transportName, Exception exception)
        {
          Core.Logging.Log.CaptureAndThrow(exception);
        }

        _cancellationToken = new CancellationTokenSource();

        var plural = (totalCount == 1) ? "" : "s";
        message = string.IsNullOrEmpty(message) ? $"Sent {totalCount} object{plural} from Dynamo" : message;

        var commitId = Functions.Functions.Send(@base, stream, _cancellationToken.Token, branchName, message,
          ProgressAction, ErrorAction);
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
        ResetUI();

        if (!_cancellationToken.IsCancellationRequested)
        {
          _hasOutput = true;
          ExpireNode();
        }
      }
    }

    private void ResetUI()
    {
      Transmitting = false;
      ExpiredCount = "";
      Message = "";
      Progress = 0;
      _hasOutput = false;
    }

    internal void UpdateExpiredCount(EngineController engine)
    {
      if (!InPorts[0].IsConnected || !InPorts[1].IsConnected)
      {
        ExpiredCount = "";
        return;
      }

      //this value is update when the RecurseInput function loops through the data, not ideal but it works
      //if we're dealing with a single Base (preconverted obj) use GetTotalChildrenCount to count its children
      _objectCount = 0;
      var data = GetInputAs<object>(engine, 0, true);
      if (data is Base @base)
      {
        _objectCount = (int) @base.GetTotalChildrenCount();
        //exclude wrapper obj.... this is a bit of a hack...
        if (_objectCount > 1) _objectCount--;
      }

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

      return (T) value;
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


    private void ExpireNode()
    {
      OnNodeModified(true);
    }

    #region events

    internal event Action OnRequestUpdates;

    protected virtual void RequestUpdates()
    {
      OnRequestUpdates?.Invoke();
    }

    void HandlePropertyChanged(object sender, PropertyChangedEventArgs e)
    {
      if (e.PropertyName != "CachedValue")
        return;

      if (!InPorts[0].IsConnected || !InPorts[1].IsConnected)
        return;

      //prevent running when opening a saved file
      if (_firstRun)
      {
        _firstRun = false;
        return;
      }

      //the node was expired manually just to output the commit info, no need to do anything!
      if (_hasOutput)
      {
        _hasOutput = false;
        return;
      }

      RequestUpdates();
    }

    #endregion

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

      return new[]
      {
        AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), AstFactory.BuildStringNode(_outputInfo))
      };

      //return OutPorts.Enumerate().Select(output => AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(output.Index), new NullNode()));
    }

    #endregion
  }
}
