using System;
using System.Collections.Generic;
using System.Linq;
using Speckle.Connectors.DUI.Utils;
using Speckle.Connectors.DUI.Objects;
using Speckle.Newtonsoft.Json;
using Speckle.Connectors.DUI.Bindings;
using Speckle.Connectors.DUI.Models.Card;

namespace Speckle.Connectors.DUI.Models;

/// <summary>
/// Encapsulates the state Speckle needs to persist in the host app's document.
/// </summary>
public abstract class DocumentModelStore : DiscriminatedObject
{
  public List<ISpeckleHostObject> SpeckleHostObjects { get; set; } = new List<ISpeckleHostObject>();

  public List<ModelCard> Models { get; set; } = new List<ModelCard>();

  private readonly JsonSerializerSettings _serializerOptions;

  protected DocumentModelStore(JsonSerializerSettings serializerOption)
  {
    _serializerOptions = serializerOption;
  }

  /// <summary>
  /// This event is triggered by each specific host app implementation of the document model store.
  /// </summary>
  // POC: unsure about the PublicAPI annotation, unsure if this changed handle should live here on the store...  :/
  public event EventHandler DocumentChanged;

  public virtual bool IsDocumentInit { get; set; }

  // TODO: not sure about this, throwing an exception, needs some thought...
  public ModelCard GetModelById(string id)
  {
    var model = Models.First(model => model.ModelCardId == id) ?? throw new ModelNotFoundException();
    return model;
  }

  protected void OnDocumentChanged() => DocumentChanged?.Invoke(this, EventArgs.Empty);

  // POC: why not IEnumerable?
  public List<SenderModelCard> GetSenders() =>
    Models.Where(model => model.TypeDiscriminator == nameof(SenderModelCard)).Cast<SenderModelCard>().ToList();

  // POC: why not IEnumerable?
  public List<ReceiverModelCard> GetReceivers() =>
    Models.Where(model => model.TypeDiscriminator == nameof(ReceiverModelCard)).Cast<ReceiverModelCard>().ToList();

  protected string Serialize()
  {
    return JsonConvert.SerializeObject(Models, _serializerOptions);
  }

  // POC: this seemms more like a IModelsDeserializer?, seems disconnected from this class
  protected List<ModelCard> Deserialize(string models)
  {
    return JsonConvert.DeserializeObject<List<ModelCard>>(models, _serializerOptions);
  }

  public abstract void WriteToFile();

  public abstract void ReadFromFile();
}
