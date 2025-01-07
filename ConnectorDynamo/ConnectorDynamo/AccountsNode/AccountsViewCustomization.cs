using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Dynamo.Controls;
using Dynamo.Models;
using Dynamo.ViewModels;
using Dynamo.Wpf;
using Speckle.Core.Credentials;

namespace Speckle.ConnectorDynamo.AccountsNode;

public class AccountsViewCustomization : INodeViewCustomization<Accounts>
{
  private DynamoViewModel dynamoViewModel;
  private DispatcherSynchronizationContext syncContext;
  private Accounts accountsNode;
  private DynamoModel dynamoModel;

  public void CustomizeView(Accounts model, NodeView nodeView)
  {
    dynamoModel = nodeView.ViewModel.DynamoViewModel.Model;
    dynamoViewModel = nodeView.ViewModel.DynamoViewModel;
    syncContext = new DispatcherSynchronizationContext(nodeView.Dispatcher);
    accountsNode = model;

    var ui = new AccountsUi();
    nodeView.inputGrid.Children.Add(ui);

    //bindings
    ui.DataContext = model;
    ui.Loaded += Loaded;
    ui.AccountsComboBox.SelectionChanged += SelectionChanged;
    ui.AccountsComboBox.DropDownOpened += AccountsComboBoxOnDropDownOpened;
  }

  private void AccountsComboBoxOnDropDownOpened(object sender, EventArgs e)
  {
    accountsNode.ClearErrorsAndWarnings();
    accountsNode.RestoreSelection();
  }

  private void Loaded(object o, RoutedEventArgs a)
  {
    Task.Run(async () =>
    {
      accountsNode.RestoreSelection();
    });
  }

  private void SelectionChanged(object sender, SelectionChangedEventArgs e)
  {
    if (e.AddedItems.Count == 0)
    {
      return;
    }

    var account = e.AddedItems[0] as Account;
    accountsNode.SelectionChanged(account);
  }

  public void Dispose() { }
}
