using System.Collections.Generic;
using DUI3.Models;
using Speckle.Core.Credentials;

namespace DUI3.Bindings;

public interface IBasicConnectorBinding : IBinding
{
  public string GetSourceApplicationName();
  public string GetSourceApplicationVersion();
  public Account[] GetAccounts(); // Remove
  public DocumentInfo GetDocumentInfo();
  public DocumentModelStore GetDocumentState();
  public List<SendFilter> GetSendFilters();
  public void AddModelToDocumentState(ModelCard model);
  public void UpdateModelInDocumentState(ModelCard model);
  public void RemoveModelFromDocumentState(ModelCard model);
}

public static class BasicConnectorBindingEvents
{
  public static readonly string DisplayToastNotification = "DisplayToastNotification";
  public static readonly string DocumentChanged = "documentChanged";
  public static readonly string FiltersNeedRefresh = "filtersNeedRefresh";
}
