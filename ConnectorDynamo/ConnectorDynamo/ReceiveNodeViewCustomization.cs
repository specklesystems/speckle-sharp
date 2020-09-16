using Dynamo.Configuration;
using Dynamo.Controls;
using Dynamo.Models;
using Dynamo.Scheduler;
using Dynamo.ViewModels;
using Dynamo.Wpf;
using System.Windows.Threading;

namespace Speckle.ConnectorDynamo
{
  public class ReceiveNodeViewCustomization : INodeViewCustomization<Receive>
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

      receiveNode.RequestChangeStreamId += UpdateStreamId;

      UpdateStreamId();
    }


    private void UpdateStreamId()
    {
      var s = dynamoViewModel.Model.Scheduler;

      // prevent data race by running on scheduler
      var t = new DelegateBasedAsyncTask(s, () =>
      {
        receiveNode.ChangeStreams(dynamoModel.EngineController);
      });

      // then update on the ui thread
      //t.ThenSend((_) =>
      //{
      //  var bmp = CreateColorRangeBitmap(colorRange);
      //  gradientImage.Source = bmp;
      //}, syncContext);

      s.ScheduleForExecution(t);
    }

    public void Dispose() { }

  }
}
