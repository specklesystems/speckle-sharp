using System.Collections.Generic;
using DUI3.Models;
using Speckle.Core.Credentials;

namespace DUI3.Bindings;

public interface IBasicConnectorBinding : IBinding
{
  public string GetSourceApplicationName();
  public string GetSourceApplicationVersion();
  public DocumentInfo GetDocumentInfo();
  public DocumentModelStore GetDocumentState();
  public void AddModel(ModelCard model);
  public void UpdateModel(ModelCard model);
  public void RemoveModel(ModelCard model);
}

public static class BasicConnectorBindingEvents
{
  public static readonly string DisplayToastNotification = "DisplayToastNotification";
  public static readonly string DocumentChanged = "documentChanged"; 
}
