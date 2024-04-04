using System.Reflection;
using ConnectorArcGIS.Utils;
using DUI3;
using DUI3.Bindings;
using DUI3.Models;
using Sentry.Reflection;

namespace ConnectorArcGIS.Bindings;

public class BasicConnectorBinding : IBasicConnectorBinding
{
  public string Name { get; set; } = "baseBinding";
  public IBridge Parent { get; set; }

  private readonly ArcGisDocumentStore _store;

  public BasicConnectorBinding(ArcGisDocumentStore store)
  {
    _store = store;
  }

  public string GetSourceApplicationName() => "ArcGIS";

  public string GetSourceApplicationVersion() => "3";

  public string GetConnectorVersion() =>
    typeof(BasicConnectorBinding).Assembly.GetNameAndVersion().Version ?? "No version";

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
    int idx = _store.Models.FindIndex(m => model.Id == m.Id);
    _store.Models[idx] = model;
  }

  public void RemoveModel(ModelCard model)
  {
    int index = _store.Models.FindIndex(m => m.Id == model.Id);
    _store.Models.RemoveAt(index);
  }

  public void HighlightModel(string modelCardId) => throw new System.NotImplementedException();
}
