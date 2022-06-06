using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Avalonia.Controls;
using DesktopUI2.Models;
using DesktopUI2.ViewModels;
using DesktopUI2.Views.Windows.Dialogs;
using Revit.Async;
using Speckle.ConnectorRevit.Entry;
using Speckle.ConnectorRevit.Storage;
using Speckle.Core.Logging;

namespace Speckle.ConnectorRevit.UI
{
  public partial class ConnectorBindingsRevit2
  {
    public override async void WriteStreamsToFile(List<StreamState> streams)
    {
      try
      {
        await RevitTask.RunAsync(
          app =>
          {
            using (Transaction t = new Transaction(CurrentDoc.Document, "Speckle Write State"))
            {
              t.Start();
              StreamStateManager2.WriteStreamStateList(CurrentDoc.Document, streams);
              t.Commit();
            }

          }).ConfigureAwait(false);
      }
      catch (Exception ex)
      {

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
      RevitApp.Application.DocumentOpened += Application_DocumentOpened;
      RevitApp.Application.DocumentClosed += Application_DocumentClosed;
      RevitApp.Application.DocumentSaved += Application_DocumentSaved;
      RevitApp.Application.DocumentSynchronizedWithCentral += Application_DocumentSynchronizedWithCentral;
      RevitApp.Application.FileExported += Application_FileExported;
      //SelectionTimer = new Timer(1400) { AutoReset = true, Enabled = true };
      //SelectionTimer.Elapsed += SelectionTimer_Elapsed;
      // TODO: Find a way to handle when document is closed via middle mouse click
      // thus triggering the focus on a new project
    }

    private void Application_FileExported(object sender, Autodesk.Revit.DB.Events.FileExportedEventArgs e)
    {
      SendScheduledStream("export");
    }

    private void Application_DocumentSynchronizedWithCentral(object sender, Autodesk.Revit.DB.Events.DocumentSynchronizedWithCentralEventArgs e)
    {
      SendScheduledStream("sync");
    }

    private void Application_DocumentSaved(object sender, Autodesk.Revit.DB.Events.DocumentSavedEventArgs e)
    {
      SendScheduledStream("save");
    }

    private async void SendScheduledStream(string slug)
    {
      try
      {
        var stream = GetStreamsInFile().FirstOrDefault(x => x.SchedulerEnabled && x.SchedulerTrigger == slug);
        if (stream == null) return;

        var progress = new ProgressViewModel();
        progress.ProgressTitle = "Sending to Speckle 🚀";
        progress.IsProgressing = true;

        var dialog = new QuickOpsDialog();
        dialog.DataContext = progress;
        dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        dialog.Show();

        await Task.Run(() => SendStream(stream, progress));
        progress.IsProgressing = false;
        dialog.Close();
        if (!progress.CancellationTokenSource.IsCancellationRequested)
        {
          Analytics.TrackEvent(stream.Client.Account, Analytics.Events.Send, new Dictionary<string, object>() { { "method", "Schedule" }, { "filter", stream.Filter.Name } });
        }

      }
      catch (Exception ex)
      {

      }
    }


    //checks whether to refresh the stream list in case the user changes active view and selects a different document
    private void RevitApp_ViewActivated(object sender, Autodesk.Revit.UI.Events.ViewActivatedEventArgs e)
    {
      try
      {

        if (e.Document == null || e.Document.IsFamilyDocument || e.PreviousActiveView == null || GetDocHash(e.Document) == GetDocHash(e.PreviousActiveView.Document))
          return;

        var streams = GetStreamsInFile();
        UpdateSavedStreams(streams);

        MainViewModel.GoHome();
      }
      catch (Exception ex)
      {

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
          return;

        //if (SpeckleRevitCommand2.MainWindow != null)
        //  SpeckleRevitCommand2.MainWindow.Hide();

        //clear saved streams if closig a doc
        if (UpdateSavedStreams != null)
          UpdateSavedStreams(new List<StreamState>());

        MainViewModel.GoHome();
      }
      catch (Exception ex)
      {

      }
    }

    // this method is triggered when there are changes in the active document
    private void Application_DocumentChanged(object sender, Autodesk.Revit.DB.Events.DocumentChangedEventArgs e)
    { }
    private void Application_DocumentCreated(object sender, Autodesk.Revit.DB.Events.DocumentCreatedEventArgs e)
    {
      //clear saved streams if opening a new doc
      if (UpdateSavedStreams != null)
        UpdateSavedStreams(new List<StreamState>());
    }

    private void Application_DocumentOpened(object sender, Autodesk.Revit.DB.Events.DocumentOpenedEventArgs e)
    {
      var streams = GetStreamsInFile();
      if (streams != null && streams.Count != 0)
      {
        SpeckleRevitCommand2.Panel.Show();
      }
      if (UpdateSavedStreams != null)
        UpdateSavedStreams(streams);

      //exit "stream view" when changing documents
      MainViewModel.GoHome();
    }


  }
}
