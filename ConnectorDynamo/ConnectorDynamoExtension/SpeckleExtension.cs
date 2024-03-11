using System;
using Dynamo.Applications.Models;
using Dynamo.ViewModels;
using Dynamo.Wpf.Extensions;
using RevitServices.Persistence;
using Speckle.ConnectorDynamo.Functions;
using Speckle.Core.Kits;
using Speckle.Core.Logging;

namespace Speckle.ConnectorDynamo.Extension;

public class SpeckleExtension : IViewExtension
{
  public string UniqueId => "B8160241-CAC4-4189-BF79-87C92914B8EC";

  public string Name => "Speckle Extension";

  public void Loaded(ViewLoadedParams viewLoadedParams)
  {
    try
    {
      var dynamoViewModel = viewLoadedParams.DynamoWindow.DataContext as DynamoViewModel;
      //var speckleWatchHandler = new SpeckleWatchHandler(dynamoViewModel.PreferenceSettings);

      if (dynamoViewModel.Model is RevitDynamoModel rdm)
      {
        rdm.RevitDocumentChanged += Rdm_RevitDocumentChanged;
        Globals.RevitDocument = DocumentManager.Instance
          .GetType()
          .GetProperty("CurrentDBDocument")
          .GetValue(DocumentManager.Instance);
      }
      //sets a read-only property using reflection WatchHandler
      //typeof(DynamoViewModel).GetProperty("WatchHandler").SetValue(dynamoViewModel, speckleWatchHandler);

      InitializeCoreSetup();
    }
    catch (Exception ex) when (!ex.IsFatal())
    {
      SpeckleLog.Logger.Fatal(ex, "Failed to load Speckle extension");
    }
  }

  private void Rdm_RevitDocumentChanged(object sender, EventArgs e)
  {
    Globals.RevitDocument = DocumentManager.Instance
      .GetType()
      .GetProperty("CurrentDBDocument")
      .GetValue(DocumentManager.Instance);
  }

  public void Dispose() { }

  public void Shutdown() { }

  public void Startup(ViewStartupParams p)
  {
    InitializeCoreSetup();
  }

  private static void InitializeCoreSetup()
  {
    var revitHostAppVersion = Utils.GetRevitHostAppVersion();
    if (revitHostAppVersion.HasValue)
    {
      // Always initialize setup with Revit values to ensure analytics are set correctly for the Parent host app.
      // Use `AnalyticsUtils` in DynamoExtensions to reroute any analytics specific to Dynamo.
      Setup.Init(HostApplications.Revit.GetVersion(revitHostAppVersion.Value), HostApplications.Revit.Slug);
    }
    else
    {
      // Setup dynamo only when it is not running within revit, hence it's RevitHostAppVersion will be null.
      Setup.Init(HostApplications.Dynamo.GetVersion(HostAppVersion.vSandbox), HostApplications.Dynamo.Slug);
    }
  }
}
