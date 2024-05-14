using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices;
using DesktopUI2;
using DesktopUI2.Models;
using DesktopUI2.ViewModels;
using Speckle.ConnectorAutocadCivil.Entry;
using Speckle.Core.Logging;
#if ADVANCESTEEL
using ASFilerObject = Autodesk.AdvanceSteel.CADAccess.FilerObject;
#endif

namespace Speckle.ConnectorAutocadCivil.UI;

public partial class ConnectorBindingsAutocad : ConnectorBindings
{
  public void RegisterAppEvents()
  {
    //// GLOBAL EVENT HANDLERS
    Application.DocumentWindowCollection.DocumentWindowActivated += Application_WindowActivated;
    Application.DocumentManager.DocumentActivated += Application_DocumentActivated;

    var layers = Application.UIBindings.Collections.Layers;
    layers.CollectionChanged += Application_LayerChanged;
  }

  public void Application_LayerChanged(object sender, EventArgs e)
  {
    UpdateSelectedStream?.Invoke();
  }

  //checks whether to refresh the stream list in case the user changes active view and selects a different document
  private void Application_WindowActivated(object sender, DocumentWindowActivatedEventArgs e)
  {
    if (e.DocumentWindow?.Document == null || UpdateSavedStreams == null)
    {
      return;
    }

    try
    {
      List<StreamState> streams = GetStreamsInFile();
      UpdateSavedStreams(streams);
    }
    catch (Exception ex) when (!ex.IsFatal())
    {
      SpeckleLog.Logger.Error(ex, "Failed to get and update current streams in file: {exceptionMessage}", ex.Message);
    }

    MainViewModel.GoHome();
  }

  private void Application_DocumentActivated(object sender, DocumentCollectionEventArgs e)
  {
    // Triggered when a document window is activated. This will happen automatically if a document is newly created or opened.
    if (e.Document == null)
    {
      SpeckleAutocadCommand.MainWindow?.Hide();

      MainViewModel.GoHome();
      return;
    }

    try
    {
      List<StreamState> streams = GetStreamsInFile();
      if (streams.Count > 0)
      {
        SpeckleAutocadCommand.CreateOrFocusSpeckle();
      }
      UpdateSavedStreams?.Invoke(streams);
      MainViewModel.GoHome();
    }
    catch (Exception ex) when (!ex.IsFatal())
    {
      SpeckleLog.Logger.Error(ex, "Failed to get and update current streams in file: {exceptionMessage}", ex.Message);
    }
  }
}
