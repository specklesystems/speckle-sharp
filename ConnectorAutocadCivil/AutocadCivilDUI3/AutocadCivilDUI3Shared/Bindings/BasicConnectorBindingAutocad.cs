using System.Linq;
using DUI3;
using DUI3.Bindings;
using Speckle.Core.Credentials;

namespace Speckle.ConnectorAutocadDUI3.Bindings;

public class BasicConnectorBindingAutocad : IBasicConnectorBinding
{
  public string Name { get; set; }
  public IBridge Parent { get; set; }
  
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
    return Core.Credentials.AccountManager.GetAccounts().ToArray();
  }

  public DocumentInfo GetDocumentInfo()
  {
    throw new System.NotImplementedException();
  }
}
