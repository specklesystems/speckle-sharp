using System;
using System.Collections.Generic;
using System.Timers;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Speckle.ConnectorRevit.Entry;
using Speckle.ConnectorRevit.Storage;
using Speckle.Core.Models;
using Speckle.DesktopUI;
using Speckle.DesktopUI.Utils;

namespace Speckle.ConnectorRevit.UI
{
  public partial class ConnectorBindingsRevit : ConnectorBindings
  {
    public static UIApplication RevitApp;

    public static UIDocument CurrentDoc => RevitApp.ActiveUIDocument;

    /// <summary>
    /// Stores the actions for the ExternalEvent handler
    /// </summary>
    public List<Action> Queue;

    public ExternalEvent Executor;

    public Timer SelectionTimer;

    public ConnectorBindingsRevit(UIApplication revitApp) : base()
    {
      RevitApp = revitApp;
      Queue = new List<Action>();
    }

    /// <summary>
    /// Sets the revit external event handler and initialises the rocket engines.
    /// </summary>
    /// <param name="eventHandler"></param>
    public void SetExecutorAndInit(ExternalEvent eventHandler)
    {
      Executor = eventHandler;

      // LOCAL STATE
      // GetStreamsInFile();

      //// GLOBAL EVENT HANDLERS
      RevitApp.ViewActivated += RevitApp_ViewActivated;
      RevitApp.Application.DocumentChanged += Application_DocumentChanged;
      RevitApp.Application.DocumentOpened += Application_DocumentOpened;
      RevitApp.Application.DocumentClosed += Application_DocumentClosed;

      SelectionTimer = new Timer(1400) { AutoReset = true, Enabled = true };
      SelectionTimer.Elapsed += SelectionTimer_Elapsed;
      // TODO: Find a way to handle when document is closed via middle mouse click
      // thus triggering the focus on a new project
    }

    private void SelectionTimer_Elapsed(object sender, ElapsedEventArgs e)
    {
      var selectedObjects = GetSelectedObjects();

      NotifyUi(new UpdateSelectionCountEvent() { SelectionCount = selectedObjects.Count });
      NotifyUi(new UpdateSelectionEvent() { ObjectIds = selectedObjects });
    }

    public override string GetHostAppName() => ConnectorRevitUtils.RevitAppName.Replace("Revit", "Revit "); //hack for ADSK store

    public override string GetDocumentId() => GetDocHash(CurrentDoc?.Document);

    private string GetDocHash(Document doc) => Utilities.hashString(doc.PathName + doc.Title, Utilities.HashingFuctions.MD5);

    public override string GetDocumentLocation() => CurrentDoc.Document.PathName;

    public override string GetActiveViewName() => CurrentDoc.Document.ActiveView.Title;

    public override string GetFileName() => CurrentDoc.Document.Title;

    public override void SelectClientObjects(string args)
    {
      // TODO!
    }

    #region app events

    //checks whether to refresh the stream list in case the user changes active view and selects a different document
    private void RevitApp_ViewActivated(object sender, Autodesk.Revit.UI.Events.ViewActivatedEventArgs e)
    {

      if (e.Document == null || e.Document.IsFamilyDocument || e.PreviousActiveView == null || GetDocHash(e.Document) == GetDocHash(e.PreviousActiveView.Document))
        return;

      var appEvent = new ApplicationEvent()
      {
        Type = ApplicationEvent.EventType.ViewActivated,
        DynamicInfo = GetStreamsInFile()
      };
      NotifyUi(appEvent);
    }

    private void Application_DocumentClosed(object sender, Autodesk.Revit.DB.Events.DocumentClosedEventArgs e)
    {
      // the DocumentClosed event is triggered AFTER ViewActivated
      // is both doc A and B are open and B is closed, this would result in wiping the list of streams retrieved for A
      // only proceed if it's the last document open (the current is null)
      if (CurrentDoc != null)
        return;

      if (SpeckleRevitCommand.Bootstrapper != null && SpeckleRevitCommand.Bootstrapper.Application != null)
        SpeckleRevitCommand.Bootstrapper.Application.MainWindow.Hide();

      var appEvent = new ApplicationEvent() { Type = ApplicationEvent.EventType.DocumentClosed };
      NotifyUi(appEvent);
    }

    // this method is triggered when there are changes in the active document
    private void Application_DocumentChanged(object sender, Autodesk.Revit.DB.Events.DocumentChangedEventArgs e)
    { }

    private void Application_DocumentOpened(object sender, Autodesk.Revit.DB.Events.DocumentOpenedEventArgs e)
    {
      var streams = GetStreamsInFile();
      if (streams != null && streams.Count != 0)
      {
        SpeckleRevitCommand.OpenOrFocusSpeckle(RevitApp);
      }

      var appEvent = new ApplicationEvent()
      {
        Type = ApplicationEvent.EventType.DocumentOpened,
        DynamicInfo = streams
      };

      NotifyUi(appEvent);
    }

    #endregion

  }
}
