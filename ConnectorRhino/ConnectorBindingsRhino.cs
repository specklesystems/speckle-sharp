using Rhino;
using Speckle.Core.Models;
using Speckle.DesktopUI;
using Speckle.DesktopUI.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SpeckleRhino
{
  class ConnectorBindingsRhino : ConnectorBindings
  {

    public RhinoDoc RhinoDoc { get; set; }

    public ConnectorBindingsRhino(RhinoDoc rhinoDoc)
    {
      RhinoDoc = rhinoDoc;
    }

    public override void AddExistingStream(string args)
    {
      //throw new NotImplementedException();
    }

    public override void AddNewStream(StreamState state)
    {
      //throw new NotImplementedException();
    }

    public override void AddObjectsToClient(string args)
    {
      //throw new NotImplementedException();
    }

    public override void BakeStream(string args)
    {
      //throw new NotImplementedException();
    }

    public override string GetActiveViewName()
    {
      return RhinoDoc.Views.ActiveView.ActiveViewport.Name;
    }

    public override string GetApplicationHostName()
    {
      return Speckle.Core.Kits.Applications.Rhino;
    }

    public override string GetDocumentId()
    {
      return Utilities.hashString("X" + RhinoDoc?.Path + RhinoDoc?.Name, Utilities.HashingFuctions.MD5);
    }

    public override string GetDocumentLocation()
    {
      return RhinoDoc?.Path;
    }

    public override List<StreamState> GetFileContext()
    {
      return new List<StreamState>();
    }

    public override string GetFileName()
    {
      return RhinoDoc?.Name;
    }

    public override List<string> GetObjectsInView()
    {
      return new List<string>();
    }

    public override List<string> GetSelectedObjects()
    {
      return new List<string>();
    }

    public override List<ISelectionFilter> GetSelectionFilters()
    {
      return new List<ISelectionFilter>();
      //throw new NotImplementedException();
    }

    public override Task<StreamState> ReceiveStream(StreamState state)
    {
      throw new NotImplementedException();
    }

    public override void RemoveObjectsFromClient(string args)
    {
      throw new NotImplementedException();
    }

    public override void RemoveSelectionFromClient(string args)
    {
      throw new NotImplementedException();
    }

    public override void RemoveStream(string args)
    {
      throw new NotImplementedException();
    }

    public override void SelectClientObjects(string args)
    {
      throw new NotImplementedException();
    }

    public override Task<StreamState> SendStream(StreamState state)
    {
      throw new NotImplementedException();
    }

    public override void UpdateStream(StreamState state)
    {
      throw new NotImplementedException();
    }
  }
}
