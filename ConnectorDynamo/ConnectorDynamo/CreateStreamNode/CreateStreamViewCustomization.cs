using Dynamo.Configuration;
using Dynamo.Controls;
using Dynamo.Models;
using Dynamo.Scheduler;
using Dynamo.ViewModels;
using Dynamo.Wpf;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Speckle.Core.Credentials;

namespace Speckle.ConnectorDynamo.CreateStreamNode
{
  public class CreateStreamViewCustomization : INodeViewCustomization<CreateStream>
  {
    private DynamoViewModel dynamoViewModel;
    private DispatcherSynchronizationContext syncContext;
    private CreateStream accountsNode;
    private DynamoModel dynamoModel;

    public void CustomizeView(CreateStream model, NodeView nodeView)
    {
      dynamoModel = nodeView.ViewModel.DynamoViewModel.Model;
      dynamoViewModel = nodeView.ViewModel.DynamoViewModel;
      syncContext = new DispatcherSynchronizationContext(nodeView.Dispatcher);
      accountsNode = model;

      var ui = new CreateStreamUi();
      nodeView.inputGrid.Children.Add(ui);

      //bindings
      ui.DataContext = model;
      ui.Loaded += Loaded;
      ui.CreateStreamButton.Click += CreateStreamButtonClick;
    }

    private void Loaded(object o, RoutedEventArgs a)
    {
      Task.Run(() => { accountsNode.RestoreSelection(); });
    }

    private void CreateStreamButtonClick(object sender, RoutedEventArgs e)
    {
      Task.Run(() => { accountsNode.DoCreateStream(); });
    }


    public void Dispose()
    {
    }
  }
}
