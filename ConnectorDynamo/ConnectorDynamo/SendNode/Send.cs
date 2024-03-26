using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using Dynamo.Engine;
using Dynamo.Graph.Nodes;
using Dynamo.Utilities;
using Newtonsoft.Json;
using ProtoCore.AST.AssociativeAST;
using ProtoCore.Mirror;
using Speckle.ConnectorDynamo.Functions;
using Speckle.Core.Credentials;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Models.Extensions;
using Speckle.Core.Transports;

namespace Speckle.ConnectorDynamo.SendNode;

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
  #region fields & props

  #region private fields

  private bool _transmitting = false;
  private string _message = "";
  private double _progress = 0;
  private string _expiredCount = "";
  private bool _sendEnabled = false;
  private int _objectCount = 0;
  private bool _hasOutput = false;
  private string _outputInfo = "";
  private bool _autoUpdate = false;
  private CancellationTokenSource _cancellationToken;

  //cached inputs
  private object _data { get; set; }
  private List<ITransport> _transports { get; set; }
  private Dictionary<ITransport, string> _branchNames { get; set; }
  private string _commitMessage { get; set; }

  internal List<StreamWrapper> _streamWrappers { get; set; }

  #endregion

  #region ui bindings

  //PUBLIC PROPERTIES
  //NOT to be saved with the file

  /// <summary>
  /// UI Binding
  /// </summary>
  [JsonIgnore]
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
  [JsonIgnore]
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
  [JsonIgnore]
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
  [JsonIgnore]
  public bool SendEnabled
  {
    get => _sendEnabled;
    set
    {
      _sendEnabled = value;
      RaisePropertyChanged("SendEnabled");
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

  #endregion

  /// <summary>
  /// JSON constructor, called on file open
  /// </summary>
  /// <param name="inPorts"></param>
  /// <param name="outPorts"></param>
  [JsonConstructor]
  private Send(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts)
    : base(inPorts, outPorts)
  {
    if (inPorts.Count() == 3)
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
    {
      AddOutputs();
    }

    ArgumentLacing = LacingStrategy.Disabled;
    this.PropertyChanged += HandlePropertyChanged;
  }

  /// <summary>
  /// Normal constructor, called when adding node to canvas
  /// </summary>
  public Send()
  {
    AddInputs();
    AddOutputs();

    RegisterAllPorts();
    ArgumentLacing = LacingStrategy.Disabled;
    this.PropertyChanged += HandlePropertyChanged;
  }

  private void AddInputs()
  {
    var defaultMessageValue = new StringNode { Value = "Automatic commit from Dynamo" };

    InPorts.Add(new PortModel(PortType.Input, this, new PortData("data", "The data to send")));
    InPorts.Add(new PortModel(PortType.Input, this, new PortData("stream", "The stream or streams to send to")));
    InPorts.Add(
      new PortModel(
        PortType.Input,
        this,
        new PortData("message", "Commit message. If left blank, one will be generated for you.", defaultMessageValue)
      )
    );
  }

  private void AddOutputs()
  {
    OutPorts.Add(
      new PortModel(PortType.Output, this, new PortData("stream", "Stream or streams pointing to the created commit"))
    );
  }

  internal void CancelSend()
  {
    _cancellationToken.Cancel();
    ResetNode();
  }

  /// <summary>
  /// Takes care of actually sending the data
  /// </summary>
  /// <param name="engine"></param>
  internal void DoSend(EngineController engine)
  {
    //double check, but can probably remove it
    if (!InPorts[0].IsConnected || !InPorts[1].IsConnected)
    {
      ResetNode(true);
      return;
    }

    //if already receiving, stop and start again
    if (Transmitting)
    {
      CancelSend();
    }

    Transmitting = true;
    Message = "Converting...";
    _cancellationToken = new CancellationTokenSource();

    try
    {
      if (_transports == null)
      {
        throw new SpeckleException("The stream provided is invalid");
      }

      Base @base = null;
      var converter = new BatchConverter();
      converter.OnError += (sender, args) =>
      {
        Warning(args.Error.ToFormattedString());
        Message = "Conversion errors";
      };

      try
      {
        @base = converter.ConvertRecursivelyToSpeckle(_data);
      }
      catch (Exception ex) when (!ex.IsFatal())
      {
        Message = "Conversion error";
        Warning(ex.ToFormattedString());
        throw new SpeckleException("Conversion error", ex);
      }

      Message = "Sending...";

      void ProgressAction(ConcurrentDictionary<string, int> dict)
      {
        //NOTE: progress set to indeterminate until the TotalChildrenCount is correct
        //var val = (double)dict.Values.Average() / totalCount;
        //Message = val.ToString("0%");
        //Progress = val * 100;


        //NOTE: remove when restoring % progress
        Message = "";
        foreach (var kvp in dict)
        {
          Message += $"{kvp.Key}: {kvp.Value} ";
        }
      }

      var hasErrors = false;

      void ErrorAction(string transportName, Exception e)
      {
        hasErrors = true;
        Message += e.ToFormattedString();

        Message = Message.Contains("401") ? "You don't have enough permissions to send to this stream." : Message;
        _cancellationToken.Cancel();
        ResetNode();
      }

      var commitIds = Functions.Functions.Send(
        @base,
        _transports,
        _cancellationToken.Token,
        _branchNames,
        _commitMessage,
        ProgressAction,
        ErrorAction
      );

      if (!hasErrors && commitIds != null)
      {
        _outputInfo = string.Join("|", commitIds.Select(x => x.ToString()));
        Message = "";
      }
    }
    catch (Exception ex) when (!ex.IsFatal())
    {
      if (!_cancellationToken.IsCancellationRequested)
      {
        _cancellationToken.Cancel();
        Message = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
        throw new SpeckleException(ex.Message, ex);
      }
    }
    finally
    {
      ResetNode();
      if (!_cancellationToken.IsCancellationRequested)
      {
        _hasOutput = true;
        ExpireNode();
      }
    }
  }

  /// <summary>
  /// Reset the node UI
  /// </summary>
  /// <param name="hardReset">If true, resets enabled status too</param>
  private void ResetNode(bool hardReset = false)
  {
    Transmitting = false;
    ExpiredCount = "";
    Progress = 0;
    _hasOutput = false;
    if (hardReset)
    {
      _transports = null;
      _branchNames = null;
      _data = null;
      _objectCount = 0;
      SendEnabled = false;
      Message = "";
      ClearErrorsAndWarnings();
    }
  }

  /// <summary>
  /// Triggered when the node inputs change
  /// Caches a copy of the inputs
  /// </summary>
  /// <param name="engine"></param>
  internal void LoadInputs(EngineController engine)
  {
    ResetNode(true);

    try
    {
      _data = GetInputAs<object>(engine, 0, true);
    }
    catch (Exception ex) when (!ex.IsFatal())
    {
      SpeckleLog.Logger.Warning(ex, "Data input is invalid");
      ResetNode(true);
      Message = "Data input is invalid";
      return;
    }

    try
    {
      _transports = new List<ITransport>();
      _streamWrappers = new List<StreamWrapper>(); // populated during TryConvertInputToTransport, not ideal but I'm lazy

      //this port accepts:
      //a stream wrapper, a url, a list of stream wrappers or a list of urls
      var inputTransport = GetInputAs<object>(engine, 1);
      var transportsDict = Utils.TryConvertInputToTransport(inputTransport);

      foreach (var transport in transportsDict)
      {
        if (transport.Key is ServerTransport)
        {
          var st = transport.Key as ServerTransport;
          _streamWrappers.Add(new StreamWrapper(st.StreamId, st.Account.userInfo.id, st.Account.serverInfo.url));
        }
      }

      _transports = transportsDict.Keys.ToList();
      _branchNames = transportsDict;
    }
    catch (Exception ex) when (!ex.IsFatal())
    {
      SpeckleLog.Logger.Warning(ex, "Send operation failed");
      //ignored
      ResetNode(true);
      Warning(ex.ToFormattedString());
      Message = "Not authorized";
      return;
    }

    if (_transports == null || _transports.Count == 0)
    {
      ResetNode(true);
      Message = "Stream is invalid";
      Warning("Input was neither a transport nor a stream.");
      return;
    }

    try
    {
      _commitMessage = InPorts[2].Connectors.Any() ? GetInputAs<string>(engine, 2) : ""; //IsConnected not working because has default value
    }
    catch (Exception ex) when (!ex.IsFatal())
    {
      Message = "Message is invalid, will skip it";
    }

    InitializeSender();
  }

  private void InitializeSender()
  {
    SendEnabled = true;

    if (_data is Base @base)
    {
      //_objectCount is updated when the RecurseInput function loops through the data, not ideal but it works
      //if we're dealing with a single Base (preconverted obj) use GetTotalChildrenCount to count its children
      _objectCount = (int)@base.GetTotalChildrenCount();
      //exclude wrapper obj.... this is a bit of a hack...
      if (_objectCount > 1)
      {
        _objectCount--;
      }
    }

    ExpiredCount = _objectCount.ToString();
    if (string.IsNullOrEmpty(Message))
    {
      Message = "Updates ready";
    }
  }

  private T GetInputAs<T>(EngineController engine, int port, bool count = false)
  {
    var valuesNode = InPorts[port].Connectors[0].Start.Owner;
    var valuesIndex = InPorts[port].Connectors[0].Start.Index;
    var astId = valuesNode.GetAstIdentifierForOutputIndex(valuesIndex).Name;
    var inputMirror = engine.GetMirror(astId);

    if (inputMirror == null || inputMirror.GetData() == null)
    {
      return default(T);
    }

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
      {
        _objectCount++;
      }
    }

    return @object;
  }

  private void ExpireNode()
  {
    OnNodeModified(true);
  }

  #region events

  internal event Action OnInputsChanged;

  /// <summary>
  /// Node inputs have changed, trigger load of new inputs
  /// </summary>
  protected virtual void RequestNewInputs()
  {
    OnInputsChanged?.Invoke();
  }

  void HandlePropertyChanged(object sender, PropertyChangedEventArgs e)
  {
    if (e.PropertyName != "CachedValue")
    {
      return;
    }

    if (!InPorts[0].IsConnected || !InPorts[1].IsConnected)
    {
      ResetNode(true);
      return;
    }

    //prevent running when opening a saved file
    // if (_firstRun)
    // {
    //   _firstRun = false;
    //   return;
    // }

    //the node was expired manually just to output the commit info, no need to do anything!
    if (_hasOutput)
    {
      _hasOutput = false;
      return;
    }

    RequestNewInputs();
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
    if (!InPorts[0].IsConnected || !InPorts[1].IsConnected || !_hasOutput)
    {
      return OutPorts
        .Enumerate()
        .Select(output => AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(output.Index), new NullNode()));
    }

    var dataFunctionCall = AstFactory.BuildFunctionCall(
      new Func<string, object>(Functions.Functions.SendData),
      new List<AssociativeNode> { AstFactory.BuildStringNode(_outputInfo) }
    );

    return new[] { AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), dataFunctionCall) };
  }

  #endregion
}
