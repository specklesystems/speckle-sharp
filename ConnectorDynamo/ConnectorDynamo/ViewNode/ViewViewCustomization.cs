using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Dynamo.Controls;
using Dynamo.Models;
using Dynamo.ViewModels;
using Dynamo.Wpf;

namespace Speckle.ConnectorDynamo.ViewNode;

public class ViewViewCustomization : INodeViewCustomization<View>
{
  private DynamoViewModel dynamoViewModel;
  private DispatcherSynchronizationContext syncContext;
  private View viewNode;
  private DynamoModel dynamoModel;

  public void CustomizeView(View model, NodeView nodeView)
  {
    dynamoModel = nodeView.ViewModel.DynamoViewModel.Model;
    dynamoViewModel = nodeView.ViewModel.DynamoViewModel;
    syncContext = new DispatcherSynchronizationContext(nodeView.Dispatcher);
    viewNode = model;

    viewNode.OnRequestUpdates += UpdateNode;

    var ui = new ViewUi();
    nodeView.inputGrid.Children.Add(ui);

    //bindings
    ui.DataContext = model;
    //ui.Loaded += model.AddedToDocument;
    ui.ViewStreamButton.Click += ViewStreamButtonClick;
  }

  private void UpdateNode()
  {
    Task.Run(async () =>
    {
      viewNode.UpdateNode(dynamoModel.EngineController);
    });
  }

  private void ViewStreamButtonClick(object sender, RoutedEventArgs e)
  {
    Task.Run(async () =>
    {
      viewNode.ViewStream();
    });
  }

  public void Dispose() { }
}
