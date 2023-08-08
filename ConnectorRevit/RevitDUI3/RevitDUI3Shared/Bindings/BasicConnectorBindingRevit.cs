using System.Linq;
using Autodesk.Revit.UI;
using DUI3;
using DUI3.Bindings;
using DUI3.Models;
using Speckle.ConnectorRevitDUI3.Utils;
using Speckle.Core.Credentials;
using Speckle.Core.Kits;

namespace Speckle.ConnectorRevitDUI3.Bindings;

public class BasicConnectorBindingRevit : IBasicConnectorBinding
{
  public string Name { get; set; } = "baseBinding";
  public IBridge Parent { get; set; }
  private static UIApplication RevitApp { get; set; }
  private static UIDocument CurrentDoc => RevitApp.ActiveUIDocument;
  private readonly RevitDocumentStore _store;
  
  public BasicConnectorBindingRevit(UIApplication revitApp, RevitDocumentStore store)
  {
    RevitApp = revitApp;
    _store = store;
    RevitApp.ViewActivated += (sender, e) =>
    {
      if (e.Document == null) return;
      if (e.PreviousActiveView?.Document.PathName == e.CurrentActiveView.Document.PathName) return;
      Parent?.SendToBrowser(BasicConnectorBindingEvents.DocumentChanged);
    };
  }
  
  public string GetSourceApplicationName()
  {
    return HostApplications.Revit.Slug;
  }

  public string GetSourceApplicationVersion()
  {
#if REVIT2020
    return "2020";
#endif
#if REVIT2023
    return "2023";
#endif
  }

  public DocumentInfo GetDocumentInfo()
  {
    if (CurrentDoc == null) return null;
    
    return new DocumentInfo
    {
      Name = CurrentDoc.Document.Title,
      Id = CurrentDoc.Document.GetHashCode().ToString(),
      Location = CurrentDoc.Document.PathName
    };
  }

  public DocumentModelStore GetDocumentState()
  {
    return _store;
  }

  public void AddModel(ModelCard model)
  {
    _store.Models.Add(model);
  }

  public void UpdateModel(ModelCard model)
  {
    var idx = _store.Models.FindIndex(m => model.Id == m.Id);
    _store.Models[idx] = model;
  }

  public void RemoveModel(ModelCard model)
  {
    throw new System.NotImplementedException();
  }
}
