using Speckle.Core.Credentials;

namespace DUI3.Bindings;

public interface IBasicConnectorBinding : IBinding
{
  public string GetSourceApplicationName();
  public string GetSourceApplicationVersion();
  public Account[] GetAccounts();
  public DocumentInfo GetDocumentInfo();
  public DocumentState GetDocumentState();
  public void SaveDocumentState(DocumentState state);
  public void AddModelToDocumentState(ModelCard model);
  public void UpdateModelInDocumentState(ModelCard model);
  public void RemoveModelFromDocumentState(ModelCard model);
}

public static class BasicConnectorBindingEvents
{
  public static readonly string DisplayToastNotification = "DisplayToastNotification";
  public static readonly string DocumentChanged = "documentChanged";
}
