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

      receiveNode.OnInputsChanged += InputsChanged;
      receiveNode.OnNewDataAvail += NewDataAvail;

      var ui = new ReceiveUi();
      nodeView.inputGrid.Children.Add(ui);

      //bindings
      ui.DataContext = model;

      ui.Loaded += Loaded;
      ui.ReceiveStreamButton.Click += ReceiveStreamButtonClick;
      ui.CancelReceiveStreamButton.Click += CancelReceiveStreamButtonClick;
    }

    private void Loaded(object o, RoutedEventArgs a)
    {
      Task.Run(async () => { receiveNode.InitializeReceiver(); });
    }


    private void InputsChanged()
    {
      Task.Run(async () => { receiveNode.LoadInputs(dynamoModel.EngineController); });
    }


    private void NewDataAvail()
    {
      Task.Run(async () => { receiveNode.DoReceive(); });
    }

    private void ReceiveStreamButtonClick(object sender, RoutedEventArgs e)
    {
      NewDataAvail();
    }

    private void CancelReceiveStreamButtonClick(object sender, RoutedEventArgs e)
    {
      receiveNode.CancelReceive();
    }

    public void Dispose()
    {
    }
  }
}
