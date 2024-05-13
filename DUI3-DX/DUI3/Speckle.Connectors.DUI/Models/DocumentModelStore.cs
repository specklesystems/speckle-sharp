using System.Collections.ObjectModel;
using Speckle.Connectors.DUI.Utils;
using Speckle.Newtonsoft.Json;
using Speckle.Connectors.DUI.Models.Card;

namespace Speckle.Connectors.DUI.Models;

/// <summary>
/// Encapsulates the state Speckle needs to persist in the host app's document.
/// </summary>
public abstract class DocumentModelStore : DiscriminatedObject
{
  public ObservableCollection<ModelCard> Models { get; set; } = new ObservableCollection<ModelCard>();

  private readonly JsonSerializerSettings _serializerOptions;

  protected DocumentModelStore(JsonSerializerSettings serializerOptions, bool writeToFileOnChange = false)
  {
    _serializerOptions = serializerOptions;
    if (writeToFileOnChange)
    {
      Models.CollectionChanged += (_, _) => WriteToFile();
    }
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

  public void UpdateModel(ModelCard model)
  {
    int idx = Models.ToList().FindIndex(m => model.ModelCardId == m.ModelCardId);
    Models[idx] = model;
  }

  public void RemoveModel(ModelCard model)
  {
    int index = Models.ToList().FindIndex(m => m.ModelCardId == model.ModelCardId);
    Models.RemoveAt(index);
  }

  protected void OnDocumentChanged() => DocumentChanged?.Invoke(this, EventArgs.Empty);

  public IEnumerable<SenderModelCard> GetSenders() =>
    Models.Where(model => model.TypeDiscriminator == nameof(SenderModelCard)).Cast<SenderModelCard>();

  public IEnumerable<ReceiverModelCard> GetReceivers() =>
    Models.Where(model => model.TypeDiscriminator == nameof(ReceiverModelCard)).Cast<ReceiverModelCard>();

  protected string Serialize()
  {
    return JsonConvert.SerializeObject(Models, _serializerOptions);
  }

  // POC: this seemms more like a IModelsDeserializer?, seems disconnected from this class
  protected ObservableCollection<ModelCard> Deserialize(string models)
  {
    return JsonConvert.DeserializeObject<ObservableCollection<ModelCard>>(models, _serializerOptions);
  }

  public abstract void WriteToFile();

  /// <summary>
  /// Reads from file the existing speckle model cards and assigns it to Models.
  /// Note: When implementing this, make sure Models gets set via assignment, and not via .Add as it will
  /// trigger the OnCollectionChanged event.
  /// </summary>
  public abstract void ReadFromFile();
}
