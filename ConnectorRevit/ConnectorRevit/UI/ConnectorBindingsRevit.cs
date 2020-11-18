using System;
using System.Timers;
using System.Collections.Generic;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
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
      GetStreamsInFile();

      //// REVIT INJECTION
      //InjectRevitAppInKits();

      //// GLOBAL EVENT HANDLERS
      RevitApp.ViewActivated += RevitApp_ViewActivated;
      RevitApp.Application.DocumentChanged += Application_DocumentChanged;
      RevitApp.Application.DocumentOpened += Application_DocumentOpened;
      RevitApp.Application.DocumentClosed += Application_DocumentClosed;


      SelectionTimer = new Timer(1400) {AutoReset = true, Enabled = true};
      SelectionTimer.Elapsed += SelectionTimer_Elapsed;
      // TODO: Find a way to handle when document is closed via middle mouse click
      // thus triggering the focus on a new project
    }

    private void SelectionTimer_Elapsed(object sender, ElapsedEventArgs e)
    {
      var selectedObjects = GetSelectedObjects();

      NotifyUi(new UpdateSelectionCountEvent() { SelectionCount = selectedObjects.Count});
      NotifyUi(new UpdateSelectionEvent() {ObjectIds = selectedObjects});
    }

    public override string GetHostAppName() => Applications.Revit;

    public override string GetDocumentId() => GetDocHash(CurrentDoc.Document);

    private string GetDocHash(Document doc) => Utilities.hashString(doc.PathName + doc.Title, Utilities.HashingFuctions.MD5);

    public override string GetDocumentLocation() => CurrentDoc.Document.PathName;

    public override string GetActiveViewName() => CurrentDoc.Document.ActiveView.Title;

    public override string GetFileName() => CurrentDoc.Document.Title;

    public override List<StreamState> GetStreamsInFile()
    {
      LocalStateWrapper = StreamStateManager.ReadState(CurrentDoc.Document);
      return LocalStateWrapper.StreamStates;
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
        new ListSelectionFilter {Name = "Category", Icon = "Category", Values = categories},
        new ListSelectionFilter {Name = "View", Icon = "RemoveRedEye", Values = views},
        new PropertySelectionFilter
        {
          Name = "Parameter",
          Icon = "FilterList",
          HasCustomProperty = false,
          Values = parameters,
          Operators = new List<string> {"equals", "contains", "is greater than", "is less than"}
        }
      };
    }

    public override void SelectClientObjects(string args)
    {
      throw new NotImplementedException();
    }

    #region app events

    private void RevitApp_ViewActivated(object sender, Autodesk.Revit.UI.Events.ViewActivatedEventArgs e)
    {
      if ( GetDocHash(e.Document) == GetDocHash(e.PreviousActiveView?.Document) ) return;

      var appEvent = new ApplicationEvent()
      {
        Type = ApplicationEvent.EventType.ViewActivated, DynamicInfo = GetStreamsInFile()
      };
      NotifyUi(appEvent);
    }

    private void Application_DocumentClosed(object sender, Autodesk.Revit.DB.Events.DocumentClosedEventArgs e)
    {
      var appEvent = new ApplicationEvent() {Type = ApplicationEvent.EventType.DocumentClosed};
      NotifyUi(appEvent);
    }

    private void Application_DocumentChanged(object sender, Autodesk.Revit.DB.Events.DocumentChangedEventArgs e)
    {
      var streamStates = GetStreamsInFile();
      if ( streamStates == LocalStateWrapper.StreamStates ) return;
      var appEvent = new ApplicationEvent()
      {
        Type = ApplicationEvent.EventType.DocumentOpened, DynamicInfo = streamStates
      };
      NotifyUi(appEvent);
    }

    private void Application_DocumentOpened(object sender, Autodesk.Revit.DB.Events.DocumentOpenedEventArgs e)
    {
      var appEvent = new ApplicationEvent()
      {
        Type = ApplicationEvent.EventType.DocumentOpened, DynamicInfo = GetStreamsInFile()
      };
      NotifyUi(appEvent);
    }

    #endregion

    private void WriteStateToFile()
    {
      Queue.Add(new Action(() =>
      {
        using ( Transaction t = new Transaction(CurrentDoc.Document, "Speckle Write State") )
        {
          t.Start();
          StreamStateManager.WriteState(CurrentDoc.Document, LocalStateWrapper);
          t.Commit();
        }
      }));
      Executor.Raise();
    }
  }
}
