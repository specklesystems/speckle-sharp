using Dynamo.Configuration;
using Dynamo.Controls;
using Dynamo.Models;
using Dynamo.Scheduler;
using Dynamo.ViewModels;
using Dynamo.Wpf;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace Speckle.ConnectorDynamo.SendNode
{
  public class SendViewCustomization : INodeViewCustomization<Send>
  {

    private DynamoViewModel dynamoViewModel;
    private DispatcherSynchronizationContext syncContext;
    private Send sendNode;
    private DynamoModel dynamoModel;

    public void CustomizeView(Send model, NodeView nodeView)
    {
      dynamoModel = nodeView.ViewModel.DynamoViewModel.Model;
      dynamoViewModel = nodeView.ViewModel.DynamoViewModel;
      syncContext = new DispatcherSynchronizationContext(nodeView.Dispatcher);
      sendNode = model;

      sendNode.OnRequestUpdates += UpdateNode;

      var ui = new SendUi();
      nodeView.inputGrid.Children.Add(ui);

      //bindings
      ui.DataContext = model;
      //ui.Loaded += model.AddedToDocument;
      ui.SendStreamButton.Click += SendStreamButtonClick;
      ui.CancelSendStreamButton.Click += CancelSendStreamButtonClick;


    }

    private void CancelSendStreamButtonClick(object sender, RoutedEventArgs e)
    {
      sendNode.CancelSend();
    }


    private void UpdateNode()
    {
      Task.Run(() =>
      {
        sendNode.UpdateExpiredCount(dynamoModel.EngineController);
      });
    }

    private void SendStreamButtonClick(object sender, RoutedEventArgs e)
    {
      Task.Run(() =>
      {
        sendNode.DoSend(dynamoModel.EngineController);
      });
    }

    public void Dispose() { }

  }
}
