using Dynamo.Extensions;
using Dynamo.ViewModels;
using Dynamo.Wpf.Extensions;
using Speckle.ConnectorDynamo.Functions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Speckle.ConnectorDynamo.Extension
{
  public class SpeckleExtension : IViewExtension
  {
    public string UniqueId => "B8160241-CAC4-4189-BF79-87C92914B8EC";

    public string Name => "Speckle Extension";

    public void Loaded(ViewLoadedParams viewLoadedParams)
    {
      Telemetry.TrackView(Telemetry.LOADED);

      try
      {
        var dynamoViewModel = viewLoadedParams.DynamoWindow.DataContext as DynamoViewModel;
        var speckleWatchHandler = new SpeckleWatchHandler(dynamoViewModel.PreferenceSettings);

        //sets a read-only property using reflection WatchHandler
        //typeof(DynamoViewModel).GetProperty("WatchHandler").SetValue(dynamoViewModel, speckleWatchHandler);

      }
      catch(Exception e)
      {

      }
    }

    public void Dispose() { }

    public void Shutdown() { }

    public void Startup(ViewStartupParams p) { }
  }
}
