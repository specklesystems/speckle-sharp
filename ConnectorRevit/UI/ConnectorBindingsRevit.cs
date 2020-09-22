using System;
using System.Timers;
using System.Collections.Generic;
using Newtonsoft.Json;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Speckle.Core.Api;
using Speckle.Core.Models;
using Speckle.DesktopUI;
using Speckle.DesktopUI.Utils;
using Speckle.ConnectorRevit.Storage;
using Speckle.Core.Kits;

namespace Speckle.ConnectorRevit.UI
{
  public partial class ConnectorBindingsRevit : ConnectorBindings
  {
    public string TestParam = "hello from Revit bindings!";
    public static UIApplication RevitApp;

    public static UIDocument CurrentDoc => RevitApp.ActiveUIDocument;

    /// <summary>
    /// Stores the actions for the ExternalEvent handler
    /// </summary>
    public List<Action> Queue;

    public ExternalEvent Executor;
    public Timer SelectionTimer;

    /// <summary>
    /// Holds the current project's streams
    /// </summary>
    public StreamStateWrapper LocalStateWrapper;

    public List<Stream> DEP_LocalState;

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

      LocalStateWrapper = new StreamStateWrapper();

      // LOCAL STATE
      DEP_LocalState = new List<Stream>();
      Queue.Add(new Action(() =>
      {
        using ( Transaction t = new Transaction(CurrentDoc.Document, "Switching Local Speckle State") )
        {
          t.Start();
          DEP_LocalState = SpeckleStateManager.ReadState(CurrentDoc.Document);
          LocalStateWrapper = StreamStateManager.ReadState(CurrentDoc.Document);
          //InjectStateInKits();
          t.Commit();
        }
      }));
      Executor.Raise();

      //// REVIT INJECTION
      //InjectRevitAppInKits();

      //// GLOBAL EVENT HANDLERS
      RevitApp.ViewActivated += RevitApp_ViewActivated;
      // RevitApp.Application.DocumentChanged += Application_DocumentChanged;
      RevitApp.Application.DocumentOpened += Application_DocumentOpened;
      RevitApp.Application.DocumentClosed += Application_DocumentClosed;
      RevitApp.Idling += ApplicationIdling;


      SelectionTimer = new Timer(1400) {AutoReset = true, Enabled = true};
      SelectionTimer.Elapsed += SelectionTimer_Elapsed;
      // TODO: Find a way to handle when document is closed via middle mouse click
      // thus triggering the focus on a new project
    }

    private void SelectionTimer_Elapsed(object sender, ElapsedEventArgs e)
    {
      if ( CurrentDoc == null ) return;
      var selectedObjectsCount = CurrentDoc != null ? CurrentDoc.Selection.GetElementIds().Count : 0;

      var updateEvent = new UpdateSelectionCountEvent() {SelectionCount = selectedObjectsCount};
      NotifyUi(updateEvent);
    }

    public override void AddObjectsToClient(string args)
    {
      // implemented in ClientOperations
    }

    public override void AddExistingStream(string args)
    {
      throw new NotImplementedException();
    }

    public override void BakeStream(string args)
    {
      throw new NotImplementedException();
    }

    public override string GetApplicationHostName()
    {
      return "Revit";
    }

    public override string GetDocumentId()
    {
      return GetDocHash(CurrentDoc.Document);
    }

    private string GetDocHash(Document doc)
    {
      return Utilities.hashString(doc.PathName + doc.Title, Utilities.HashingFuctions.MD5);
    }

    public override string GetDocumentLocation()
    {
      return CurrentDoc.Document.PathName;
    }

    public override List<StreamState> GetFileContext()
    {
      var states = StreamStateManager.ReadState(CurrentDoc.Document) ?? new StreamStateWrapper();
      LocalStateWrapper = states;

      return states.StreamStates;
    }

    public override string GetFileName()
    {
      return CurrentDoc.Document.Title;
    }

    public override List<ISelectionFilter> GetSelectionFilters()
    {
      var categories = new List<string>();
      var parameters = new List<string>();
      var views = new List<string>();
      if ( CurrentDoc != null )
      {
        //selectionCount = CurrentDoc.Selection.GetElementIds().Count();
        categories = Globals.GetCategoryNames(CurrentDoc.Document);
        parameters = Globals.GetParameterNames(CurrentDoc.Document);
        views = Globals.GetViewNames(CurrentDoc.Document);
      }


      return new List<ISelectionFilter>
      {
        new ElementsSelectionFilter
        {
          Name = "Selection",
          Icon = "Mouse",
          Selection = new List<string>()
        },
        new ListSelectionFilter
        {
          Name = "Category",
          Icon = "Category",
          Values = categories
        },
        new ListSelectionFilter
        {
          Name = "View",
          Icon = "RemoveRedEye",
          Values = views
        },
        new PropertySelectionFilter
        {
          Name = "Parameter",
          Icon = "FilterList",
          HasCustomProperty = false,
          Values = parameters,
          Operators = new List<string>
          {
            "equals",
            "contains",
            "is greater than",
            "is less than"
          }
        }
      };
    }

    public override void RemoveStream(string args)
    {
      throw new NotImplementedException();
    }

    public override void RemoveObjectsFromClient(string args)
    {
      // implemented in ClientOperations
    }

    public override void SelectClientObjects(string args)
    {
      throw new NotImplementedException();
    }

    #region app events

    private void RevitApp_ViewActivated(object sender, Autodesk.Revit.UI.Events.ViewActivatedEventArgs e)
    {
      if ( GetDocHash(e.Document) != GetDocHash(e.PreviousActiveView.Document) )
      {
        // DispatchStoreActionUi("flushClients");
        var streamStates = GetFileContext();
        LocalStateWrapper.StreamStates = streamStates;
        var appEvent = new ApplicationEvent()
        {
          Type = ApplicationEvent.EventType.ViewActivated,
          DynamicInfo = streamStates
        };
        NotifyUi(appEvent);

        Queue.Add(new Action(() =>
        {
          using ( Transaction t = new Transaction(CurrentDoc.Document, "Switching Local Speckle State") )
          {
            t.Start();
            DEP_LocalState = SpeckleStateManager.ReadState(CurrentDoc.Document);
            // InjectStateInKits();
            t.Commit();
          }
        }));
        Executor.Raise();
      }
    }

    private void Application_DocumentClosed(object sender, Autodesk.Revit.DB.Events.DocumentClosedEventArgs e)
    {
      // DispatchStoreActionUi("flushClients");
      var appEvent = new ApplicationEvent()
      {
        Type = ApplicationEvent.EventType.DocumentClosed
      };
      NotifyUi(appEvent);
    }

    private void Application_DocumentOpened(object sender, Autodesk.Revit.DB.Events.DocumentOpenedEventArgs e)
    {
      // DispatchStoreActionUi("flushClients");
      var streamStates = GetFileContext();
      LocalStateWrapper.StreamStates = streamStates;
      var appEvent = new ApplicationEvent()
      {
        Type = ApplicationEvent.EventType.DocumentOpened,
        DynamicInfo = streamStates
      };
      NotifyUi(appEvent);

      Queue.Add(new Action(() =>
      {
        using ( Transaction t = new Transaction(CurrentDoc.Document, "Reading Local Speckle State") )
        {
          t.Start();
          DEP_LocalState = SpeckleStateManager.ReadState(CurrentDoc.Document);
          // InjectStateInKits();
          t.Commit();
        }
      }));
      Executor.Raise();
    }

    private void ApplicationIdling(object sender, Autodesk.Revit.UI.Events.IdlingEventArgs e)
    {
      // var appEvent = new ApplicationEvent()
      // {
      //   Type = ApplicationEvent.EventType.ApplicationIdling,
      //   DynamicInfo = GetFileContext()
      // };
      // NotifyUi(appEvent);
    }


    #endregion
  }
}