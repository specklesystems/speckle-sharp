using System;
using System.Collections.Generic;
using System.Text;

namespace ConnectorRevit.Revit
{
  public class EventHandling
  {
    #region app events

    //checks whether to refresh the stream list in case the user changes active view and selects a different document
    private void RevitApp_ViewActivated(object sender, Autodesk.Revit.UI.Events.ViewActivatedEventArgs e)
    {

      if (e.Document == null || e.Document.IsFamilyDocument || e.PreviousActiveView == null || GetDocHash(e.Document) == GetDocHash(e.PreviousActiveView.Document))
        return;



      //var appEvent = new ApplicationEvent()
      //{
      //  Type = ApplicationEvent.EventType.ViewActivated,
      //  DynamicInfo = GetStreamsInFile()
      //};
      //NotifyUi(appEvent);
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

      //var appEvent = new ApplicationEvent() { Type = ApplicationEvent.EventType.DocumentClosed };
      // NotifyUi(appEvent);
    }

    // this method is triggered when there are changes in the active document
    private void Application_DocumentChanged(object sender, Autodesk.Revit.DB.Events.DocumentChangedEventArgs e)
    { }

    private void Application_DocumentOpened(object sender, Autodesk.Revit.DB.Events.DocumentOpenedEventArgs e)
    {
      var streams = GetStreamsInFile();
      if (streams.Any())
      {
        SpeckleRevitCommand2.CreateOrFocusSpeckle();
      }
    }

    public override List<MenuItem> GetCustomStreamMenuItems()
    {
      return new List<MenuItem>();
    }

    #endregion
  }
}
