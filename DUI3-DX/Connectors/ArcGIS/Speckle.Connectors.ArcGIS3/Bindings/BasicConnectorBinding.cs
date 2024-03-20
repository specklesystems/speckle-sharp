using System.Reflection;
using Sentry.Reflection;
using Speckle.Connectors.ArcGIS.HostApp;
using Speckle.Connectors.ArcGIS.Utils;
using Speckle.Connectors.DUI.Bindings;
using Speckle.Connectors.DUI.Bridge;
using Speckle.Connectors.DUI.Models;
using Speckle.Connectors.DUI.Models.Card;
using Speckle.Core.Kits;

namespace Speckle.Connectors.ArcGIS.Bindings;

public class BasicConnectorBinding : IBasicConnectorBinding
{
  public string Name { get; set; } = "baseBinding";
  public IBridge Parent { get; set; }

  public BasicConnectorBindingCommands Commands { get; }
  private readonly ArcGISDocumentStore _store;
  private readonly ArcGISSettings _settings;

  public BasicConnectorBinding(ArcGISDocumentStore store, ArcGISSettings settings, IBridge parent)
  {
    _store = store;
    _settings = settings;
    Parent = parent;
    Commands = new BasicConnectorBindingCommands(parent);

    _store.DocumentChanged += (_, _) =>
    {
      Commands.NotifyDocumentChanged();
    };
  }

  public string GetSourceApplicationName() => "ArcGIS"; // _settings.HostAppInfo.Slug;

  public string GetSourceApplicationVersion() => "3"; //_settings.HostAppInfo.GetVersion(HostAppVersion version);

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
