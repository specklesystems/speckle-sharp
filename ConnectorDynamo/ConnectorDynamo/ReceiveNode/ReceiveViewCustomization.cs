using Dynamo.Configuration;
using Dynamo.Controls;
using Dynamo.Models;
using Dynamo.Scheduler;
using Dynamo.ViewModels;
using Dynamo.Wpf;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace Speckle.ConnectorDynamo.ReceiveNode
{
  public class ReceiveViewCustomization : INodeViewCustomization<Receive>
  {

    private DynamoViewModel dynamoViewModel;
    private DispatcherSynchronizationContext syncContext;
    private Receive receiveNode;
    private DynamoModel dynamoModel;

    public void CustomizeView(Receive model, NodeView nodeView)
    {
      dynamoModel = nodeView.ViewModel.DynamoViewModel.Model;
      dynamoViewModel = nodeView.ViewModel.DynamoViewModel;
      syncContext = new DispatcherSynchronizationContext(nodeView.Dispatcher);
      receiveNode = model;

      receiveNode.RequestChangeStream += UpdateStream;
      receiveNode.RequestReceive += RequestReceive;

      UpdateStream();

      var ui = new ReceiveUi();
      nodeView.inputGrid.Children.Add(ui);

      //bindings
      ui.DataContext = model;
      //ui.Loaded += model.AddedToDocument;
      ui.ReceiveStreamButton.Click += ReceiveStreamButtonClick;
      ui.CancelReceiveStreamButton.Click += CancelReceiveStreamButtonClick;
    }

    private void CancelReceiveStreamButtonClick(object sender, RoutedEventArgs e)
    {
      receiveNode.CancelReceive();
    }

    private void UpdateStream()
    {
      Task.Run(() =>
      {
        receiveNode.ChangeStreams(dynamoModel.EngineController);
      });
    }

    private void ReceiveStreamButtonClick(object sender, RoutedEventArgs e)
    {
      RequestReceive();
    }

    private void RequestReceive()
    {
      Task.Run(() =>
      {
        receiveNode.DoReceive(dynamoModel.EngineController);
      });
    }

    public void Dispose() { }

  }
}
