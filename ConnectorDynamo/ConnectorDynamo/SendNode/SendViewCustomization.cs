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

      sendNode.OnInputsChanged += InputsChanged;

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

    private DebounceTimer debounceTimer = new DebounceTimer();

    private void InputsChanged()
    {
      debounceTimer.Debounce(300,
        () =>
        {
          Task.Run(async () =>
          {
            sendNode.LoadInputs(dynamoModel.EngineController);
            if (sendNode.AutoUpdate)
              sendNode.DoSend(dynamoModel.EngineController);
          });
        });
    }

    private void SendStreamButtonClick(object sender, RoutedEventArgs e)
    {
      Task.Run(async () => { sendNode.DoSend(dynamoModel.EngineController); });
    }

    public void Dispose()
    {
    }
  }
}
