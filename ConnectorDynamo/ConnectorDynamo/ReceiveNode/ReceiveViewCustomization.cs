using Dynamo.Controls;
using Dynamo.Models;
using Dynamo.ViewModels;
using Dynamo.Wpf;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Speckle.Core.Helpers;

namespace Speckle.ConnectorDynamo.ReceiveNode;

public class ReceiveViewCustomization : INodeViewCustomization<Receive>
{
  private DynamoViewModel dynamoViewModel;
  private DispatcherSynchronizationContext syncContext;
  private Receive receiveNode;
  private NodeView _nodeView;
  private DynamoModel dynamoModel;
  private MenuItem viewStreamMenuItem;

  public void CustomizeView(Receive model, NodeView nodeView)
  {
    _nodeView = nodeView;
    dynamoModel = nodeView.ViewModel.DynamoViewModel.Model;
    dynamoViewModel = nodeView.ViewModel.DynamoViewModel;
    syncContext = new DispatcherSynchronizationContext(nodeView.Dispatcher);
    receiveNode = model;

    receiveNode.OnInputsChanged += InputsChanged;
    receiveNode.OnNewDataAvail += NewDataAvail;

    var ui = new ReceiveUi();
    nodeView.inputGrid.Children.Add(ui);

    //bindings
    ui.DataContext = model;

    ui.Loaded += Loaded;
    ui.ReceiveStreamButton.Click += ReceiveStreamButtonClick;
    ui.CancelReceiveStreamButton.Click += CancelReceiveStreamButtonClick;

    //nodeView.grid.ContextMenu.Items.Add(new Separator());
  }

  private void Loaded(object o, RoutedEventArgs a)
  {
    Task.Run(async () =>
    {
      receiveNode.InitializeReceiver();
    });
  }

  private DebounceTimer debounceTimer = new();

  private void InputsChanged()
  {
    debounceTimer.Debounce(
      300,
      () =>
      {
        Task.Run(async () =>
        {
          receiveNode.LoadInputs(dynamoModel.EngineController);
          UpdateContextMenu();
        });
      }
    );
  }

  private void NewDataAvail()
  {
    Task.Run(async () =>
    {
      receiveNode.DoReceive();
    });
  }

  private void ReceiveStreamButtonClick(object sender, RoutedEventArgs e)
  {
    NewDataAvail();
  }

  private void UpdateContextMenu()
  {
    receiveNode.DispatchOnUIThread(() =>
    {
      if (viewStreamMenuItem != null)
      {
        _nodeView.grid.ContextMenu.Items.Remove(viewStreamMenuItem);
      }

      viewStreamMenuItem = null;

      if (receiveNode.Stream != null)
      {
        viewStreamMenuItem = new MenuItem
        {
          Header = $"View stream {receiveNode.Stream.StreamId} @ {receiveNode.Stream.ServerUrl} online ↗"
        };
        viewStreamMenuItem.Click += (a, e) =>
        {
          Open.Url($"{receiveNode.Stream.ServerUrl}/streams/{receiveNode.Stream.StreamId}");
        };
        _nodeView.grid.ContextMenu.Items.Add(viewStreamMenuItem);
      }
    });
  }

  private void CancelReceiveStreamButtonClick(object sender, RoutedEventArgs e)
  {
    receiveNode.CancelReceive();
  }

  public void Dispose() { }
}
