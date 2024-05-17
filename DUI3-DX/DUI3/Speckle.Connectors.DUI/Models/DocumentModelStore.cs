using System.Collections.ObjectModel;
using Speckle.Connectors.DUI.Utils;
using Speckle.Newtonsoft.Json;
using Speckle.Connectors.DUI.Models.Card;

namespace Speckle.Connectors.DUI.Models;

/// <summary>
/// Encapsulates the state Speckle needs to persist in the host app's document.
/// </summary>
public abstract class DocumentModelStore
{
  private ObservableCollection<ModelCard> _models = new();

  /// <summary>
  /// Stores all the model cards in the current document/file.
  /// </summary>
  public ObservableCollection<ModelCard> Models
  {
    get => _models;
    protected set
    {
      _models = value;
      RegisterWriteOnChangeEvent();
    }
  }

  private readonly JsonSerializerSettings _serializerOptions;

  private readonly bool _writeToFileOnChange;

  /// <summary>
  /// Base host app state class that controls the storage of the models in the file.
  /// </summary>
  /// <param name="serializerOptions">our custom serialiser that should be globally DI'ed in.</param>
  /// <param name="writeToFileOnChange">Whether to store the models state in the file on any change. Defaults to false out of caution, but it's recommended to set to true, unless severe host app limitations.</param>
  protected DocumentModelStore(JsonSerializerSettings serializerOptions, bool writeToFileOnChange = false)
  {
    _serializerOptions = serializerOptions;
    _writeToFileOnChange = writeToFileOnChange;

    RegisterWriteOnChangeEvent();
  }

  private void RegisterWriteOnChangeEvent()
  {
    if (_writeToFileOnChange)
    {
      _models.CollectionChanged += (_, _) => WriteToFile();
    }
  }

  /// <summary>
  /// This event is triggered by each specific host app implementation of the document model store.
  /// </summary>
  // POC: unsure about the PublicAPI annotation, unsure if this changed handle should live here on the store...  :/
  public event EventHandler? DocumentChanged;

  public virtual bool IsDocumentInit { get; set; }

  // TODO: not sure about this, throwing an exception, needs some thought...
  // Further note (dim): If we reach to the stage of throwing an exception here because a model is not found, there's a huge misalignment between the UI's list of model cards and the host app's.
  // In theory this should never really happen, but if it does
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
  protected ObservableCollection<ModelCard>? Deserialize(string models)
  {
    return JsonConvert.DeserializeObject<ObservableCollection<ModelCard>>(models, _serializerOptions);
  }

  /// <summary>
  /// Implement this method according to the host app's specific ways of storing custom data in its file.
  /// </summary>
  public abstract void WriteToFile();

  /// <summary>
  /// Implement this method according to the host app's specific ways of reading custom data from its file.
  /// </summary>
  public abstract void ReadFromFile();
}
