using System.Linq;
using DUI3;
using DUI3.Bindings;
using DUI3.Filters;
using Rhino;
using Speckle.Core.Credentials;

namespace ConnectorRhinoWebUI.Bindings
{
  public class BasicConnectorBindingRhino : IBasicConnectorBinding
  {
    public string Name { get; set; } = "baseBinding";
    public IBridge Parent { get; set; }
    public BasicConnectorBindingRhino()
    {
      RhinoDoc.EndOpenDocumentInitialViewUpdate += (sender, e) =>
      {
        if (e.Merge) return;
        if (e.Document == null) return;
        Parent?.SendToBrowser(BasicConnectorBindingEvents.DocumentChanged);
      };
    }

    public string GetSourceApplicationName()
    {
      return "Rhino";
    }

    public string GetSourceApplicationVersion()
    {
      return "7";
    }

    public Account[] GetAccounts()
    {
      return AccountManager.GetAccounts().ToArray();
    }

    public DocumentInfo GetDocumentInfo()
    {
      return new DocumentInfo
      {
        Location = RhinoDoc.ActiveDoc.Path,
        Name = RhinoDoc.ActiveDoc.Name,
        Id = RhinoDoc.ActiveDoc.RuntimeSerialNumber.ToString()
      };
    }

    public string[] GetSelectedObjects()
    {
      var objectIds = RhinoDoc.ActiveDoc.Objects.GetSelectedObjects(false, false).Select(obj => obj.Id.ToString()).ToArray();
      return objectIds;
    }

    public ISendFilter[] GetAvailableFilters()
    {
      
      return null;
    }
    
    // public void Send(string projectId, string modelId, object filter)
    // {
    //   const objectIdsToSend = filter.GetObjects();
    // }
    
    // OR 
    // basically we assume we do filter.getObjects() in the ui

    // public void Send(string projectId, string modelId, string[] objectIds)
    // {
    //   
    // }
  }
}

