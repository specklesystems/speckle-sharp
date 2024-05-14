using System.Reflection;
using ArcGIS.Desktop.Core;
using Speckle.Connectors.ArcGIS.HostApp;
using Speckle.Connectors.DUI.Bindings;
using Speckle.Connectors.DUI.Bridge;
using Speckle.Connectors.DUI.Models;
using Speckle.Connectors.DUI.Models.Card;
using Speckle.Connectors.Utils.Reflection;

namespace Speckle.Connectors.ArcGIS.Bindings;

//poc: dupe code between connectors
public class BasicConnectorBinding : IBasicConnectorBinding
{
  public string Name => "baseBinding";
  public IBridge Parent { get; }

  public BasicConnectorBindingCommands Commands { get; }
  private readonly DocumentModelStore _store;
  private readonly ArcGISSettings _settings;

  public BasicConnectorBinding(DocumentModelStore store, ArcGISSettings settings, IBridge parent)
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

  public string GetSourceApplicationName() => _settings.HostAppInfo.Slug;

  public string GetSourceApplicationVersion() => _settings.HostAppInfo.GetVersion(_settings.HostAppVersion);

  public string GetConnectorVersion() => Assembly.GetAssembly(GetType())!.GetVersion();

  // TODO
  public DocumentInfo GetDocumentInfo() =>
    new()
    {
      Location = Project.Current.URI,
      Name = Project.Current.Name,
      Id = Project.Current.Name,
    };

  public DocumentModelStore GetDocumentState() => _store;

  public void AddModel(ModelCard model) => _store.Models.Add(model);

  public void UpdateModel(ModelCard model) => _store.UpdateModel(model);

  public void RemoveModel(ModelCard model) => _store.RemoveModel(model);

  public void HighlightModel(string modelCardId) => throw new System.NotImplementedException();
}
