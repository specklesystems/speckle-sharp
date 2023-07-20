using System.Linq;
using Autodesk.Revit.UI;
using DUI3;
using Speckle.Core.Credentials;
using Speckle.Core.Kits;

namespace Speckle.ConnectorRevitDUI3.Bindings;

public class RevitBaseBinding : IBasicConnectorBinding
{
  public string Name { get; set; } = "baseBinding";
  public IBridge Parent { get; set; }
  private static UIApplication RevitApp { get; set; }
  private static UIDocument CurrentDoc => RevitApp.ActiveUIDocument;
  public RevitBaseBinding(UIApplication revitApp)
  {
    RevitApp = revitApp;

    RevitApp.ViewActivated += (sender, e) =>
    {
      if (e.Document == null) return;
      if (e.PreviousActiveView.Document.PathName == e.CurrentActiveView.Document.PathName) return;
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

  public Account[] GetAccounts()
  {
    return AccountManager.GetAccounts().ToArray();
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
}
