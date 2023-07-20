using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using DUI3;
using DUI3.Bindings;
using Speckle.Core.Credentials;

namespace Speckle.ConnectorAutocadDUI3.Bindings;

public class BasicConnectorBindingAutocad : IBasicConnectorBinding
{
  public string Name { get; set; } = "baseBinding";
  public IBridge Parent { get; set; }

  private static Document Doc => Application.DocumentManager.MdiActiveDocument;
  private static string _previousDocName;
  
  public BasicConnectorBindingAutocad()
  {
    Application.DocumentWindowCollection.DocumentWindowActivated += (sender, e) => NotifyDocumentChangedIfNeeded(e.DocumentWindow.Document as Document);
    Application.DocumentManager.DocumentActivated += (sender, e) => NotifyDocumentChangedIfNeeded(e.Document);
  }

  private void NotifyDocumentChangedIfNeeded(Document doc)
  {
    if (doc == null) return;
    if (_previousDocName == doc.Name) return;
      
    _previousDocName = doc.Name;
    Parent?.SendToBrowser(BasicConnectorBindingEvents.DocumentChanged);
  }

  public string GetSourceApplicationName()
  {
    return Core.Kits.HostApplications.AutoCAD.Slug;
  }

  public string GetSourceApplicationVersion()
  {
    #if AUTOCAD2023DUI3
    return "2023";
    # endif
    #if AUTOCAD2022DUI3
    return "2022";
    #endif
  }

  public Account[] GetAccounts()
  {
    return AccountManager.GetAccounts().ToArray();
  }

  public DocumentInfo GetDocumentInfo()
  {
    var name = Doc.Name.Split(System.IO.Path.PathSeparator).Reverse().First();
    return new DocumentInfo()
    {
      Name = name,
      Id = Doc.Name,
      Location = Doc.Name
    };
  }
}
