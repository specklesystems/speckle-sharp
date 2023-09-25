using System;
using Autodesk.AutoCAD.ApplicationServices;
using DesktopUI2;
using DesktopUI2.ViewModels;
using Speckle.ConnectorAutocadCivil.Entry;

#if ADVANCESTEEL
using ASFilerObject = Autodesk.AdvanceSteel.CADAccess.FilerObject;
#endif

namespace Speckle.ConnectorAutocadCivil.UI
{
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
      if (UpdateSelectedStream != null)
        UpdateSelectedStream();
    }

    //checks whether to refresh the stream list in case the user changes active view and selects a different document
    private void Application_WindowActivated(object sender, DocumentWindowActivatedEventArgs e)
    {
      try
      {
        if (e.DocumentWindow.Document == null || UpdateSavedStreams == null)
          return;

        var streams = GetStreamsInFile();
        UpdateSavedStreams(streams);

        MainViewModel.GoHome();
      }
      catch { }
    }

    private void Application_DocumentActivated(object sender, DocumentCollectionEventArgs e)
    {
      try
      {
        // Triggered when a document window is activated. This will happen automatically if a document is newly created or opened.
        if (e.Document == null)
        {
          if (SpeckleAutocadCommand.MainWindow != null)
            SpeckleAutocadCommand.MainWindow.Hide();

          MainViewModel.GoHome();
          return;
        }

        var streams = GetStreamsInFile();
        if (streams.Count > 0)
          SpeckleAutocadCommand.CreateOrFocusSpeckle();

        if (UpdateSavedStreams != null)
          UpdateSavedStreams(streams);

        MainViewModel.GoHome();
      }
      catch { }
    }
  }
}
