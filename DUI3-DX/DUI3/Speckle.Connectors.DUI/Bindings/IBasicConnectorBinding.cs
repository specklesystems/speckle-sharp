using System;
using Speckle.Connectors.DUI.Bridge;
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

  public BasicConnectorBindingCommands Commands { get; }
}

public static class BasicConnectorBindingEvents
{
  public const string DISPLAY_TOAST_NOTIFICATION = "DisplayToastNotification";
  public const string DOCUMENT_CHANGED = "documentChanged";
}

public class BasicConnectorBindingCommands
{
  private const string NOTIFY_DOCUMENT_CHANGED_EVENT_NAME = "documentChanged";
  private const string SET_MODEL_PROGRESS_UI_COMMAND_NAME = "setModelProgress";
  private const string SET_MODEL_ERROR_UI_COMMAND_NAME = "setModelError";

  protected IBridge _bridge;

  public BasicConnectorBindingCommands(IBridge bridge)
  {
    _bridge = bridge;
  }

  public void NotifyDocumentChanged() => _bridge.Send(NOTIFY_DOCUMENT_CHANGED_EVENT_NAME);

  public void SetModelProgress(string modelCardId, ModelCardProgress progress) =>
    _bridge.Send(SET_MODEL_PROGRESS_UI_COMMAND_NAME, new { modelCardId, progress });

  public void SetModelError(string modelCardId, Exception error) =>
    _bridge.Send(SET_MODEL_ERROR_UI_COMMAND_NAME, new { modelCardId, error = error.Message });
}
