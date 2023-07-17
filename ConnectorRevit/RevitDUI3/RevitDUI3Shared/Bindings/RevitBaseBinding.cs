using System.Linq;
using Autodesk.Revit.UI;
using DUI3;
using Speckle.Core.Credentials;

namespace Speckle.ConnectorRevitDUI3.Bindings;

public class RevitBaseBinding : IBasicConnectorBinding
{
  public string Name { get; set; } = "baseBinding";
  public IBridge Parent { get; set; }
  
  public static UIApplication RevitApp;
  
  public RevitBaseBinding(UIApplication revitApp=null)
  {
    // TODO: set up doc events, etc.
    // RevitApp = revitApp;
  }
  
  public string GetSourceApplicationName()
  {
    return "Revit";
  }

  public string GetSourceApplicationVersion()
  {
    return "2020";
  }

  public Account[] GetAccounts()
  {
    return Speckle.Core.Credentials.AccountManager.GetAccounts().ToArray();
  }

  public DocumentInfo GetDocumentInfo()
  {
    return new DocumentInfo
    {
      Name = "test",
      Id = "test",
      Location = "test"
    };
  }
}
