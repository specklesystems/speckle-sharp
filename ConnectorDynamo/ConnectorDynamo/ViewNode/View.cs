using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using Dynamo.Engine;
using Dynamo.Graph.Nodes;
using Dynamo.Utilities;
using Newtonsoft.Json;
using ProtoCore.AST.AssociativeAST;
using Speckle.Core.Logging;

namespace Speckle.ConnectorDynamo.ViewNode
{
  /// <summary>
  /// Send data to Speckle
  /// </summary>
  [NodeName("View Online")]
  [NodeCategory("Speckle 2.Stream")]
  [NodeDescription("View Stream online")]
  [NodeSearchTags("view", "speckle")]
  [IsDesignScriptCompatible]
  public class View : NodeModel
  {
    public string Url { get; set; }
    private bool _viewEnabled = false;


    /// <summary>
    /// UI Binding
    /// </summary>
    [JsonIgnore]
    public bool ViewEnabled
    {
      get => _viewEnabled;
      set
      {
        _viewEnabled = value;
        RaisePropertyChanged("ViewEnabled");
      }
    }


    /// <summary>
    /// JSON constructor, called on file open
    /// </summary>
    /// <param name="inPorts"></param>
    /// <param name="outPorts"></param>
    [JsonConstructor]
    private View(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
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
    public View()
    {
      AddInputs();

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
      InPorts.Add(new PortModel(PortType.Input, this, new PortData("stream or url", "The stream to view")));
    }


    /// <summary>
    /// Takes care of actually sending the data
    /// </summary>
    /// <param name="engine"></param>
    internal void ViewStream()
    {
      if (!InPorts[0].IsConnected || string.IsNullOrEmpty(Url))
      {
        ViewEnabled = false;
        Url = "";
        return;
      }

      Analytics.TrackEvent(Analytics.Events.NodeRun, new Dictionary<string, object>() { { "name", "Stream View" } });

      Process.Start(Url);
    }

    //Cache input value each time it comes available
    internal void UpdateNode(EngineController engine)
    {
      if (!InPorts[0].IsConnected)
      {
        ViewEnabled = false;
        return;
      }

      var url = GetInputAsString(engine, 0);

      if (string.IsNullOrEmpty(url))
      {
        ViewEnabled = false;
        Url = "";
        return;
      }

      Uri uriResult;
      bool result = Uri.TryCreate(url, UriKind.Absolute, out uriResult)
                    && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

      if (!result)
      {
        ViewEnabled = false;
        Url = "";
        return;
      }

      ViewEnabled = true;
      Url = url;
    }


    private string GetInputAsString(EngineController engine, int port, bool count = false)
    {
      var valuesNode = InPorts[port].Connectors[0].Start.Owner;
      var valuesIndex = InPorts[port].Connectors[0].Start.Index;
      var astId = valuesNode.GetAstIdentifierForOutputIndex(valuesIndex).Name;
      var inputMirror = engine.GetMirror(astId);

      if (inputMirror == null || inputMirror.GetData() == null) return null;

      var data = inputMirror.GetData().Data?.ToString();

      return data;
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

      if (!InPorts[0].IsConnected)
        return;


      RequestUpdates();
    }

    void Connectors_CollectionChanged(object sender,
      System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
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
      return OutPorts.Enumerate().Select(output =>
        AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(output.Index), new NullNode()));
    }

    #endregion
  }
}
