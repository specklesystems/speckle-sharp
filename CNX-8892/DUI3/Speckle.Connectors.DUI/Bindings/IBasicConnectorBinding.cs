using Speckle.Connectors.DUI.Models;
using Speckle.Connectors.DUI.Models.Card;

namespace Speckle.Connectors.DUI.Bindings;

public interface IBasicConnectorBinding : IBinding
{
  public string GetSourceApplicationName();
  public string GetSourceApplicationVersion();
  public string GetConnectorVersion();
  public DocumentInfo GetDocumentInfo();
  public DocumentModelStore GetDocumentState();
  public void AddModel(ModelCard model);
  public void UpdateModel(ModelCard model);
  public void RemoveModel(ModelCard model);

  /// <summary>
  /// Highlights the objects attached to this sender in the host application.
  /// </summary>
  /// <param name="modelCardId"></param>
  public void HighlightModel(string modelCardId);
}

public static class BasicConnectorBindingEvents
{
  public const string DISPLAY_TOAST_NOTIFICATION = "DisplayToastNotification";
  public const string DOCUMENT_CHANGED = "documentChanged";
}
