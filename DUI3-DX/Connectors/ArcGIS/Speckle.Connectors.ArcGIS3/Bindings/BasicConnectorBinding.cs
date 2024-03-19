using System.Reflection;
using ConnectorArcGIS.Utils;
using Sentry.Reflection;
using Speckle.Connectors.DUI;
using Speckle.Connectors.DUI.Bindings;
using Speckle.Connectors.DUI.Bridge;
using Speckle.Connectors.DUI.Models;
using Speckle.Connectors.DUI.Models.Card;

namespace ConnectorArcGIS.Bindings;

public class BasicConnectorBinding : IBasicConnectorBinding
{
  public string Name { get; set; } = "baseBinding";
  public IBridge Parent { get; set; }

  public BasicConnectorBindingCommands Commands => throw new System.NotImplementedException();

  private readonly ArcGisDocumentStore _store;

  public BasicConnectorBinding(ArcGisDocumentStore store)
  {
    _store = store;
  }

  public string GetSourceApplicationName() => "ArcGIS";

  public string GetSourceApplicationVersion() => "3";

  public string GetConnectorVersion() => Assembly.GetAssembly(GetType()).GetNameAndVersion().Version;

  // TODO
  public DocumentInfo GetDocumentInfo() =>
    new()
    {
      Location = "",
      Name = "",
      Id = ""
    };

  public DocumentModelStore GetDocumentState() => _store;

  public void AddModel(ModelCard model) => _store.Models.Add(model);

  public void UpdateModel(ModelCard model)
  {
    int idx = _store.Models.FindIndex(m => model.ModelCardId == m.ModelCardId);
    _store.Models[idx] = model;
  }

  public void RemoveModel(ModelCard model)
  {
    int index = _store.Models.FindIndex(m => m.ModelCardId == model.ModelCardId);
    _store.Models.RemoveAt(index);
  }

  public void HighlightModel(string modelCardId) => throw new System.NotImplementedException();
}
