using System;
using DUI3.Models;
using DUI3.Models.Card;

namespace DUI3.Bindings;

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

public static class BasicConnectorBindingCommands
{
  private const string NOTIFY_DOCUMENT_CHANGED_EVENT_NAME = "documentChanged";
  private const string SET_MODEL_PROGRESS_UI_COMMAND_NAME = "setModelProgress";
  private const string SET_MODEL_ERROR_UI_COMMAND_NAME = "setModelError";

  public static void NotifyDocumentChanged(IBridge bridge) => bridge.SendToBrowser(NOTIFY_DOCUMENT_CHANGED_EVENT_NAME);
  
  public static void SetModelProgress(IBridge bridge,string modelCardId, ModelCardProgress progress) => 
    bridge.SendToBrowser(SET_MODEL_PROGRESS_UI_COMMAND_NAME, new { modelCardId, progress });
  
  public static void SetModelError(IBridge bridge, string modelCardId, Exception error) =>
    bridge.SendToBrowser(SET_MODEL_ERROR_UI_COMMAND_NAME, new { modelCardId, error = error.Message });
}
