using System;
using Dynamo.Controls;
using Dynamo.Models;
using Dynamo.ViewModels;
using Dynamo.Wpf;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Speckle.Core.Helpers;

namespace Speckle.ConnectorDynamo.CreateStreamNode;

public class CreateStreamViewCustomization : INodeViewCustomization<CreateStream>
{
  private DynamoViewModel dynamoViewModel;
  private DispatcherSynchronizationContext syncContext;
  private CreateStream createNode;
  private DynamoModel dynamoModel;
  private NodeView _nodeView;

  public void CustomizeView(CreateStream model, NodeView nodeView)
  {
    _nodeView = nodeView;
    dynamoModel = nodeView.ViewModel.DynamoViewModel.Model;
    dynamoViewModel = nodeView.ViewModel.DynamoViewModel;
    syncContext = new DispatcherSynchronizationContext(nodeView.Dispatcher);
    createNode = model;

    var ui = new CreateStreamUi();
    nodeView.inputGrid.Children.Add(ui);

    //bindings
    ui.DataContext = model;
    ui.Loaded += Loaded;
    ui.CreateStreamButton.Click += CreateStreamButtonClick;
    ui.AccountsComboBox.DropDownOpened += AccountsComboBoxOnDropDownOpened;

    //nodeView.grid.ContextMenu.Items.Add(new Separator());
  }

  private void AccountsComboBoxOnDropDownOpened(object sender, EventArgs e)
  {
    createNode.ClearErrorsAndWarnings();
    createNode.RestoreSelection();
  }

  private void Loaded(object o, RoutedEventArgs a)
  {
    Task.Run(async () =>
    {
      createNode.RestoreSelection();
    });
  }

  private void CreateStreamButtonClick(object sender, RoutedEventArgs e)
  {
    Task.Run(async () =>
    {
      createNode.DoCreateStream();
      UpdateContextMenu();
    });
  }

  private void UpdateContextMenu()
  {
    createNode.DispatchOnUIThread(() =>
    {
      if (createNode.Stream != null)
      {
        var viewStreamMenuItem = new MenuItem
        {
          Header = $"View stream {createNode.Stream.StreamId} @ {createNode.Stream.ServerUrl} online ↗"
        };
        viewStreamMenuItem.Click += (a, e) =>
        {
          Open.Url($"{createNode.Stream.ServerUrl}/streams/{createNode.Stream.StreamId}");
        };
        _nodeView.grid.ContextMenu.Items.Add(viewStreamMenuItem);
      }
    });
  }

  public void Dispose() { }
}
