using Speckle.Core.Credentials;

namespace DUI3.Bindings;

public interface IBasicConnectorBinding : IBinding
{
  public string GetSourceApplicationName();
  public string GetSourceApplicationVersion();
  public Account[] GetAccounts();
  public DocumentInfo GetDocumentInfo();
}

public static class BasicConnectorBindingEvents
{
  public static readonly string DisplayToastNotification = "DisplayToastNotification";
  public static readonly string DocumentChanged = "documentChanged";
}
