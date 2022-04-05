using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using Dynamo.Graph.Nodes;
using Dynamo.Utilities;
using Newtonsoft.Json;
using ProtoCore.AST.AssociativeAST;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Logging;
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

    public string SelectedUserId = "";


    /// <summary>
    /// UI Binding
    /// </summary>
    [JsonIgnore]
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
    [JsonIgnore]
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
    [JsonConstructor]
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
        SelectedUserId = "";
        return;
      }

      SelectedAccount = !string.IsNullOrEmpty(SelectedUserId)
        ? AccountList.FirstOrDefault(x => x.userInfo.id == SelectedUserId)
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

      var client = new Client(SelectedAccount);
      try
      {
        var res = client.StreamCreate(new StreamCreateInput { isPublic = false }).Result;

        Stream = new StreamWrapper(res, SelectedAccount.userInfo.id, SelectedAccount.serverInfo.url);
        CreateEnabled = false;
        SelectedUserId = SelectedAccount.userInfo.id;

        this.Name = "Stream Created";

        Analytics.TrackEvent(SelectedAccount, Analytics.Events.NodeRun, new Dictionary<string, object>() { { "name", "Stream Create" } });

        OnNodeModified(true);
      }
      catch (Exception ex)
      {
        //someone improve this pls :)
        if (ex.InnerException != null && ex.InnerException.InnerException != null)
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

      var sw = AstFactory.BuildStringNode(Stream.ToString());

      return new[] { AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), sw) };
    }

    #endregion
  }
}
