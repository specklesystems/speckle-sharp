using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Timers;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using DesktopUI2;
using DesktopUI2.Models;
using Speckle.ConnectorRevit.Entry;
using Speckle.ConnectorRevit.Storage;
using Speckle.Core.Models;
using Timer = System.Timers.Timer;

namespace Speckle.ConnectorRevit.UI
{
  public partial class ConnectorBindingsRevit2 : ConnectorBindings
  {
    public static UIApplication RevitApp;

    public static UIDocument CurrentDoc => RevitApp.ActiveUIDocument;

    public Timer SelectionTimer;

    public ConnectorBindingsRevit2(UIApplication revitApp) : base()
    {
      RevitApp = revitApp;
    }



    private void SelectionTimer_Elapsed(object sender, ElapsedEventArgs e)
    {
      var selectedObjects = GetSelectedObjects();

      //TODO

      //NotifyUi(new UpdateSelectionCountEvent() { SelectionCount = selectedObjects.Count });
      //NotifyUi(new UpdateSelectionEvent() { ObjectIds = selectedObjects });
    }

    public override string GetHostAppName() => ConnectorRevitUtils.RevitAppName.Replace("Revit", "Revit "); //hack for ADSK store

    public override string GetDocumentId() => GetDocHash(CurrentDoc?.Document);

    private string GetDocHash(Document doc) => Utilities.hashString(doc.PathName + doc.Title, Utilities.HashingFuctions.MD5);

    public override string GetDocumentLocation() => CurrentDoc.Document.PathName;

    public override string GetActiveViewName() => CurrentDoc.Document.ActiveView.Title;

    public override string GetFileName() => CurrentDoc.Document.Title;

    public override void SelectClientObjects(string args)
    {
      throw new NotImplementedException();
    }


    public override List<MenuItem> GetCustomStreamMenuItems()
    {
      return new List<MenuItem>();
    }


  }
}
