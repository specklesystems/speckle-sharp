using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using DesktopUI2;
using DesktopUI2.Models;
using DesktopUI2.ViewModels;
using Revit.Async;
using Speckle.ConnectorRevit.Entry;
using Speckle.ConnectorRevit.Storage;



namespace Speckle.ConnectorRevit.UI
{
  public partial class ConnectorBindingsRevit2
  {
    public override async void WriteStreamsToFile(List<StreamState> streams)
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

        });
    }

    /// <summary>
    /// Sets the revit external event handler and initialises the rocket engines.
    /// </summary>
    /// <param name="eventHandler"></param>
    public void RegisterAppEvents()
    {

      //// GLOBAL EVENT HANDLERS
      RevitApp.ViewActivated += RevitApp_ViewActivated;
      RevitApp.Application.DocumentChanged += Application_DocumentChanged;
      RevitApp.Application.DocumentOpened += Application_DocumentOpened;
      RevitApp.Application.DocumentClosed += Application_DocumentClosed;
      //SelectionTimer = new Timer(1400) { AutoReset = true, Enabled = true };
      //SelectionTimer.Elapsed += SelectionTimer_Elapsed;
      // TODO: Find a way to handle when document is closed via middle mouse click
      // thus triggering the focus on a new project
    }


    //checks whether to refresh the stream list in case the user changes active view and selects a different document
    private void RevitApp_ViewActivated(object sender, Autodesk.Revit.UI.Events.ViewActivatedEventArgs e)
    {

      if (e.Document == null || e.Document.IsFamilyDocument || e.PreviousActiveView == null || GetDocHash(e.Document) == GetDocHash(e.PreviousActiveView.Document))
        return;

      var streams = GetStreamsInFile();
      UpdateSavedStreams(streams);

    }

    private void Application_DocumentClosed(object sender, Autodesk.Revit.DB.Events.DocumentClosedEventArgs e)
    {
      // the DocumentClosed event is triggered AFTER ViewActivated
      // is both doc A and B are open and B is closed, this would result in wiping the list of streams retrieved for A
      // only proceed if it's the last document open (the current is null)
      if (CurrentDoc != null)
        return;

      if (SpeckleRevitCommand2.MainWindow != null)
        SpeckleRevitCommand2.MainWindow.Hide();

    }

    // this method is triggered when there are changes in the active document
    private void Application_DocumentChanged(object sender, Autodesk.Revit.DB.Events.DocumentChangedEventArgs e)
    { }

    private void Application_DocumentOpened(object sender, Autodesk.Revit.DB.Events.DocumentOpenedEventArgs e)
    {
      var streams = GetStreamsInFile();
      if (streams != null && streams.Count != 0)
      {
        SpeckleRevitCommand2.CreateOrFocusSpeckle();
      }
      if(UpdateSavedStreams!=null)
        UpdateSavedStreams(streams);
    }


  }
}
