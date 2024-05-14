using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Events;
using Avalonia.Controls;
using DesktopUI2.Models;
using DesktopUI2.ViewModels;
using DesktopUI2.Views.Windows.Dialogs;
using RevitSharedResources.Models;
using Speckle.ConnectorRevit.Entry;
using Speckle.ConnectorRevit.Storage;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using RevitSharedResources.Extensions.SpeckleExtensions;

namespace Speckle.ConnectorRevit.UI;

public partial class ConnectorBindingsRevit
{
  private string _lastSyncComment { get; set; }

  public override async void WriteStreamsToFile(List<StreamState> streams)
  {
    try
    {
      await APIContext
        .Run(app =>
        {
          using (Transaction t = new(CurrentDoc.Document, "Speckle Write State"))
          {
            t.Start();
            StreamStateManager.WriteStreamStateList(CurrentDoc.Document, streams);
            t.Commit();
          }
        })
        .ConfigureAwait(false);
    }
    // note : I don't think this exception catch is necessary, but this is an async VOID instead
    // of an async Task. Revit WILL CRASH if this method tries to throw an error, so keeping this
    // catch out of an abundance of caution
    catch (Exception ex) when (!ex.IsFatal())
    {
      SpeckleLog.Logger.Fatal(
        ex,
        "Swallowing exception in {methodName}: {exceptionMessage}",
        nameof(WriteStreamsToFile),
        ex.Message
      );
    }
  }

  /// <summary>
  /// Sets the revit external event handler and initialises the rocket engines.
  /// </summary>
  /// <param name="eventHandler"></param>
  public void RegisterAppEvents()
  {
    //// GLOBAL EVENT HANDLERS
    RevitApp.ViewActivated += RevitApp_ViewActivated;
    //RevitApp.Application.DocumentChanged += Application_DocumentChanged;
    RevitApp.Application.DocumentCreated += Application_DocumentCreated;
    RevitApp.Application.DocumentCreating += Application_DocumentCreating;
    RevitApp.Application.DocumentOpened += Application_DocumentOpened;
    // This code may fire during the DocumentOpening event, in which case an exception will be
    // thrown about being unable to subscribe to an event during that event
    //RevitApp.Application.DocumentOpening += Application_DocumentOpening;
    ;
    RevitApp.Application.DocumentClosed += Application_DocumentClosed;
    RevitApp.Application.DocumentSaved += Application_DocumentSaved;
    RevitApp.Application.DocumentSynchronizingWithCentral += Application_DocumentSynchronizingWithCentral;
    RevitApp.Application.DocumentSynchronizedWithCentral += Application_DocumentSynchronizedWithCentral;
    RevitApp.Application.FileExported += Application_FileExported;
    RevitApp.ApplicationClosing += RevitApp_ApplicationClosing;
    //RevitApp.Application.FileExporting += Application_FileExporting;
    //RevitApp.Application.FileImporting += Application_FileImporting;
    //SelectionTimer = new Timer(1400) { AutoReset = true, Enabled = true };
    //SelectionTimer.Elapsed += SelectionTimer_Elapsed;
    // TODO: Find a way to handle when document is closed via middle mouse click
    // thus triggering the focus on a new project
  }

  private void RevitApp_ApplicationClosing(object sender, Autodesk.Revit.UI.Events.ApplicationClosingEventArgs e)
  {
    if (HomeViewModel.Instance == null)
    {
      return;
    }

    ///ensure WS connections etc are disposed, otherwise it might throw
    HomeViewModel.Instance.SavedStreams.ForEach(s => s.Dispose());
  }

  //DISABLED
  private void Application_FileExporting(object sender, FileExportingEventArgs e)
  {
    ShowImportExportAlert();
  }

  //DISABLED
  private void Application_FileImporting(object sender, FileImportingEventArgs e)
  {
    ShowImportExportAlert();
  }

  private void ShowImportExportAlert()
  {
    var config = ConfigManager.Load();
    if (config.ShowImportExportAlert)
    {
      Analytics.TrackEvent(Analytics.Events.ImportExportAlert, new Dictionary<string, object>() { { "name", "Show" } });
      var dialog = new ImportExportAlert();
      dialog.LaunchAction = () =>
      {
        SpeckleRevitCommand.RegisterPane();
        var panel = App.AppInstance.GetDockablePane(SpeckleRevitCommand.PanelId);
        panel.Show();
      };
      dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
      dialog.Show();
      dialog.Topmost = true;
    }
  }

  private void Application_DocumentOpening(object sender, Autodesk.Revit.DB.Events.DocumentOpeningEventArgs e) { }

  private void Application_DocumentCreating(object sender, Autodesk.Revit.DB.Events.DocumentCreatingEventArgs e) { }

  private void Application_FileExported(object sender, Autodesk.Revit.DB.Events.FileExportedEventArgs e)
  {
    SendScheduledStream("export");
  }

  private void Application_DocumentSynchronizingWithCentral(
    object sender,
    Autodesk.Revit.DB.Events.DocumentSynchronizingWithCentralEventArgs e
  )
  {
    _lastSyncComment = e.Comments;
  }

  private void Application_DocumentSynchronizedWithCentral(
    object sender,
    Autodesk.Revit.DB.Events.DocumentSynchronizedWithCentralEventArgs e
  )
  {
    SendScheduledStream("sync", _lastSyncComment);
  }

  private void Application_DocumentSaved(object sender, Autodesk.Revit.DB.Events.DocumentSavedEventArgs e)
  {
    SendScheduledStream("save");
  }

  private async void SendScheduledStream(string slug, string message = "")
  {
    try
    {
      var stream = GetStreamsInFile().FirstOrDefault(x => x.SchedulerEnabled && x.SchedulerTrigger == slug);
      if (stream == null)
      {
        return;
      }

      var progress = new ProgressViewModel();
      progress.ProgressTitle = "Sending to Speckle 🚀";
      progress.IsProgressing = true;

      var dialog = new QuickOpsDialog();
      dialog.DataContext = progress;
      dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
      dialog.Topmost = true;
      dialog.Show();

      if (message != null)
      {
        stream.CommitMessage = message;
      }

      await Task.Run(() => SendStream(stream, progress));
      progress.IsProgressing = false;
      dialog.Close();
      if (!progress.CancellationToken.IsCancellationRequested)
      {
        Analytics.TrackEvent(
          stream.Client.Account,
          Analytics.Events.Send,
          new Dictionary<string, object>() { { "method", "Schedule" }, { "filter", stream.Filter.Name } }
        );
      }
    }
    catch (Exception ex) when (!ex.IsFatal())
    {
      SpeckleLog.Logger.LogDefaultError(ex);
    }
  }

  //checks whether to refresh the stream list in case the user changes active view and selects a different document
  private void RevitApp_ViewActivated(object sender, Autodesk.Revit.UI.Events.ViewActivatedEventArgs e)
  {
    try
    {
      if (
        e.Document == null
        || e.PreviousActiveView == null
        || e.Document.GetHashCode() == e.PreviousActiveView.Document.GetHashCode()
      )
      {
        return;
      }

      // if the dialog body is open, then avalonia will freak out and crash Revit when trying to re-initialize
      // so we need to close the dialog and cancel any ongoing send / receive operation. (Maybe we can somehow
      // save the operation state and let the user come back to it later)
      if (MainViewModel.Instance.DialogBody != null)
      {
        CurrentOperationCancellation?.Cancel();
        MainViewModel.CloseDialog();
      }

      // invalidate all revit elements in the cache
      revitDocumentAggregateCache.InvalidateAll();

      SpeckleRevitCommand.RegisterPane();

      var streams = GetStreamsInFile();
      UpdateSavedStreams(streams);

      MainViewModel.Instance.NavigateToDefaultScreen();
    }
    catch (Exception ex) when (!ex.IsFatal())
    {
      SpeckleLog.Logger.LogDefaultError(ex);
    }
  }

  private void Application_DocumentClosed(object sender, Autodesk.Revit.DB.Events.DocumentClosedEventArgs e)
  {
    try
    {
      // the DocumentClosed event is triggered AFTER ViewActivated
      // is both doc A and B are open and B is closed, this would result in wiping the list of streams retrieved for A
      // only proceed if it's the last document open (the current is null)
      if (CurrentDoc != null)
      {
        return;
      }

      //if (SpeckleRevitCommand2.MainWindow != null)
      //  SpeckleRevitCommand2.MainWindow.Hide();

      //clear saved streams if closig a doc
      if (UpdateSavedStreams != null)
      {
        UpdateSavedStreams(new List<StreamState>());
      }

      MainViewModel.Instance.NavigateToDefaultScreen();
    }
    catch (Exception ex) when (!ex.IsFatal())
    {
      SpeckleLog.Logger.LogDefaultError(ex);
    }
  }

  // this method is triggered when there are changes in the active document
  private void Application_DocumentChanged(object sender, Autodesk.Revit.DB.Events.DocumentChangedEventArgs e) { }

  private void Application_DocumentCreated(object sender, Autodesk.Revit.DB.Events.DocumentCreatedEventArgs e)
  {
    SpeckleRevitCommand.RegisterPane();

    //clear saved streams if opening a new doc
    if (UpdateSavedStreams != null)
    {
      UpdateSavedStreams(new List<StreamState>());
    }
  }

  private void Application_DocumentOpened(object sender, Autodesk.Revit.DB.Events.DocumentOpenedEventArgs e)
  {
    var streams = GetStreamsInFile();
    if (streams != null && streams.Count != 0)
    {
      if (SpeckleRevitCommand.UseDockablePanel)
      {
        SpeckleRevitCommand.RegisterPane();
        var panel = App.AppInstance.GetDockablePane(SpeckleRevitCommand.PanelId);
        panel.Show();
      }
      else
      {
        SpeckleRevitCommand.CreateOrFocusSpeckle();
      }
    }
    if (UpdateSavedStreams != null)
    {
      UpdateSavedStreams(streams);
    }

    //exit "stream view" when changing documents
    MainViewModel.Instance.NavigateToDefaultScreen();
  }

  public override bool CanOpen3DView => true;

  public override async Task Open3DView(List<double> viewCoordinates, string viewName = "")
  {
    var views = new FilteredElementCollector(CurrentDoc.Document).OfClass(typeof(View3D)).ToElements().Cast<View3D>();
    var viewtypes = new FilteredElementCollector(CurrentDoc.Document)
      .OfClass(typeof(ViewFamilyType))
      .ToElements()
      .Cast<ViewFamilyType>()
      .Where(x => x.ViewFamily == ViewFamily.ThreeDimensional);

    //hacky but the current comments camera is not a Base object
    //so it cannot be passed automatically to the converter
    //making a dummy one here
    var speckleCamera = new Base();
    speckleCamera["isHackySpeckleCamera"] = true;
    speckleCamera["coordinates"] = viewCoordinates;

    //when in a perspective view, it's not possible to open any transaction (txs adsk)
    //so we're switching to any other non perspective view here
    if (CurrentDoc.ActiveView.ViewType == ViewType.ThreeD)
    {
      var activeView = CurrentDoc.ActiveView as View3D;
      if (activeView.IsPerspective)
      {
        var nonPerspectiveView = views.FirstOrDefault(x => !x.IsPerspective);
        if (nonPerspectiveView != null)
        {
          CurrentDoc.ActiveView = nonPerspectiveView;
        }
      }
    }

    var perspView = views.FirstOrDefault(o => o.Name == "SpeckleCommentView");

    await APIContext.Run(app =>
    {
      using (var t = new Transaction(CurrentDoc.Document, $"Open Comment View"))
      {
        t.Start();

        var converter = (ISpeckleConverter)Activator.CreateInstance(Converter.GetType());
        converter.SetContextDocument(CurrentDoc.Document);
        var viewOrientation3D = converter.ConvertToNative(speckleCamera) as ViewOrientation3D;

        //txs bcfier
        if (perspView == null)
        {
          perspView = View3D.CreatePerspective(CurrentDoc.Document, viewtypes.First().Id);
          perspView.Name = "SpeckleCommentView";
        }
        perspView.SetOrientation(viewOrientation3D);
        perspView.CropBoxActive = false;
        perspView.CropBoxVisible = false;
        perspView.DisplayStyle = DisplayStyle.Shading;

        // the default phase was not looking good, picking the one of the View3D
        if (views.Any())
        {
          var viewPhase = views.First().get_Parameter(BuiltInParameter.VIEW_PHASE);
          if (viewPhase != null)
          {
            perspView.get_Parameter(BuiltInParameter.VIEW_PHASE).Set(viewPhase.AsElementId());
          }
        }

        t.Commit();
      }
      // needs to be outside the transaction
      CurrentDoc.ActiveView = perspView;
      // "refresh" the active view, txs Connor
      var uiView = CurrentDoc.GetOpenUIViews().FirstOrDefault(uv => uv.ViewId.Equals(perspView.Id));
      uiView.Zoom(1);
    });
  }
}
