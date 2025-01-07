using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Dynamo.Controls;
using Dynamo.Models;
using Dynamo.ViewModels;
using Dynamo.Wpf;
using Speckle.Core.Helpers;

namespace Speckle.ConnectorDynamo.SendNode;

public class SendViewCustomization : INodeViewCustomization<Send>
{
  private DynamoViewModel dynamoViewModel;
  private DispatcherSynchronizationContext syncContext;
  private Send sendNode;
  private DynamoModel dynamoModel;
  private NodeView _nodeView;
  private List<MenuItem> customMenuItems = new();

  public void CustomizeView(Send model, NodeView nodeView)
  {
    _nodeView = nodeView;
    dynamoModel = nodeView.ViewModel.DynamoViewModel.Model;
    dynamoViewModel = nodeView.ViewModel.DynamoViewModel;
    syncContext = new DispatcherSynchronizationContext(nodeView.Dispatcher);
    sendNode = model;

    sendNode.OnInputsChanged += InputsChanged;

    var ui = new SendUi();
    nodeView.inputGrid.Children.Add(ui);

    //bindings
    ui.DataContext = model;
    //ui.Loaded += model.AddedToDocument;
    ui.SendStreamButton.Click += SendStreamButtonClick;
    ui.CancelSendStreamButton.Click += CancelSendStreamButtonClick;

    //nodeView.grid.ContextMenu.Items.Add(new Separator());
  }

  private void CancelSendStreamButtonClick(object sender, RoutedEventArgs e)
  {
    sendNode.CancelSend();
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
          sendNode.LoadInputs(dynamoModel.EngineController);
          UpdateContextMenu();
          if (sendNode.AutoUpdate)
          {
            sendNode.DoSend(dynamoModel.EngineController);
          }
        });
      }
    );
  }

  private void UpdateContextMenu()
  {
    sendNode.DispatchOnUIThread(() =>
    {
      foreach (var item in customMenuItems)
      {
        _nodeView.grid.ContextMenu.Items.Remove(item);
      }

      customMenuItems.Clear();

      foreach (var stream in sendNode._streamWrappers)
      {
        var viewStream = new MenuItem { Header = $"View stream {stream.StreamId} @ {stream.ServerUrl} online ↗" };
        viewStream.Click += (a, e) =>
        {
          Open.Url($"{stream.ServerUrl}/streams/{stream.StreamId}");
        };
        customMenuItems.Add(viewStream);
        _nodeView.grid.ContextMenu.Items.Add(viewStream);
      }
    });
  }

  private void SendStreamButtonClick(object sender, RoutedEventArgs e)
  {
    Task.Run(async () =>
    {
      sendNode.DoSend(dynamoModel.EngineController);
    });
  }

  public void Dispose() { }
}
