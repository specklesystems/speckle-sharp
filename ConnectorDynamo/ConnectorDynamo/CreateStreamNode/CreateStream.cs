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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Threading;
using System.Windows;
using Dynamo.Utilities;
using Speckle.Core.Api;
using Speckle.Core.Models;
using Account = Speckle.Core.Credentials.Account;

namespace Speckle.ConnectorDynamo.CreateStreamNode
{
  /// <summary>
  /// Create Stream
  /// </summary>
  [NodeName("Create Stream")]
  [NodeCategory("Speckle 2.Stream.Create")]
  [NodeDescription("Create a new Speckle Stream")]
  [NodeSearchTags("stream", "create", "speckle")]
  [IsDesignScriptCompatible]
  public class CreateStream : NodeModel
  {
    private bool _createEnabled = true;

    private ObservableCollection<Core.Credentials.Account> _accountList = new ObservableCollection<Account>();

    /// <summary>
    /// Current Stream
    /// </summary>
    public StreamWrapper Stream { get; set; }

    public string SelectedAccountId = "";


    /// <summary>
    /// UI Binding
    /// </summary>
    [DNJ.JsonIgnore]
    public ObservableCollection<Core.Credentials.Account> AccountList
    {
      get => _accountList;
      set
      {
        _accountList = value;
        RaisePropertyChanged("AccountList");
      }
    }

    private Account _selectedAccount;

    /// <summary>
    /// UI Binding
    /// </summary>
    [DNJ.JsonIgnore]
    public Account SelectedAccount
    {
      get => _selectedAccount;
      set
      {
        _selectedAccount = value;
        RaisePropertyChanged("SelectedAccount");
      }
    }

    /// <summary>
    /// UI Binding
    /// </summary>
    public bool CreateEnabled
    {
      get => _createEnabled;
      set
      {
        _createEnabled = value;
        RaisePropertyChanged("CreateEnabled");
      }
    }


    /// <summary>
    /// JSON constructor, called on file open
    /// </summary>
    /// <param name="inPorts"></param>
    /// <param name="outPorts"></param>
    [DNJ.JsonConstructor]
    private CreateStream(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
    {
      if (outPorts.Count() == 0)
        AddOutputs();

      ArgumentLacing = LacingStrategy.Disabled;
    }

    /// <summary>
    /// Normal constructor, called when adding node to canvas
    /// </summary>
    public CreateStream()
    {
      AddOutputs();

      RegisterAllPorts();
      ArgumentLacing = LacingStrategy.Disabled;
    }

    private void AddOutputs()
    {
      OutPorts.Add(new PortModel(PortType.Output, this, new PortData("stream", "The new Stream")));
    }

    internal void RestoreSelection()
    {
      AccountList = new ObservableCollection<Account>(AccountManager.GetAccounts());
      if (AccountList.Count == 0)
      {
        Warning("No accounts found. Please use the Speckle Manager to manage your accounts on this computer.");
        SelectedAccount = null;
        SelectedAccountId = "";
        return;
      }

      SelectedAccount = !string.IsNullOrEmpty(SelectedAccountId) 
        ? AccountList.FirstOrDefault(x => x.id == SelectedAccountId) 
        : AccountList.FirstOrDefault(x => x.isDefault);
    }

    internal void DoCreateStream()
    {
      ClearErrorsAndWarnings();

      if (SelectedAccount == null)
      {
        Error("An account must be selected.");
        return;
      }

      Tracker.TrackPageview(Tracker.STREAM_CREATE);

      var client = new Client(SelectedAccount);
      try
      {
        var res = client.StreamCreate(new StreamCreateInput()).Result;

        Stream = new StreamWrapper(res, SelectedAccount.id, SelectedAccount.serverInfo.url);
        CreateEnabled = false;
        SelectedAccountId = SelectedAccount.id;

        this.Name = "Stream Created";
        OnNodeModified(true);
      }
      catch(Exception ex)
      {
        //someone improve this pls :)
        if(ex.InnerException !=null && ex.InnerException.InnerException !=null)
          Error(ex.InnerException.InnerException.Message);
        if (ex.InnerException != null)
          Error(ex.InnerException.Message);
        else
          Error(ex.Message);

      }

    }


    #region overrides

    /// <summary>
    /// Returns the StreamWrapper saved in this node
    /// To create a new stream, a new node has to be placed
    /// </summary>
    /// <param name="inputAstNodes"></param>
    /// <returns></returns>
    public override IEnumerable<AssociativeNode> BuildOutputAst(List<AssociativeNode> inputAstNodes)
    {
      if (Stream == null)
        return OutPorts.Enumerate().Select(output =>
          AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(output.Index), new NullNode()));

      var functionCall = AstFactory.BuildFunctionCall(
        new Func<string, string, StreamWrapper>(Functions.Stream.GetByStreamAndAccountId),
        new List<AssociativeNode>
        {
          AstFactory.BuildStringNode(Stream.StreamId), AstFactory.BuildStringNode(Stream.AccountId)
        });

      return new[] { AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), functionCall) };
    }

    #endregion
  }
}
