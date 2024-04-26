using System;
using System.Collections.Generic;
using System.Linq;
using DUI3.Bindings;
using DUI3.Objects;
using DUI3.Utils;
using JetBrains.Annotations;
using Speckle.Newtonsoft.Json;

namespace DUI3.Models;

/// <summary>
/// Encapsulates the state Speckle needs to persist in the host app's document.
/// </summary>
public abstract class DocumentModelStore : DiscriminatedObject
{
  // POC: public setter?
  public List<ISpeckleHostObject> SpeckleHostObjects { get; set; } = new List<ISpeckleHostObject>();

  // POC: public setter?
  public List<ModelCard> Models { get; set; } = new List<ModelCard>();

  private static readonly JsonSerializerSettings s_serializerOptions =
    DUI3.Utils.SerializationSettingsFactory.GetSerializerSettings();

  /// <summary>
  /// This event is triggered by each specific host app implementation of the document model store.
  /// </summary>
  [PublicAPI]
  public event EventHandler DocumentChanged;

  public virtual bool IsDocumentInit { get; set; }

  public ModelCard GetModelById(string id)
  {
    var model = Models.First(model => model.ModelCardId == id) ?? throw new ModelNotFoundException();
    return model;
  }

  protected void OnDocumentChanged() => DocumentChanged?.Invoke(this, EventArgs.Empty);

  public List<SenderModelCard> GetSenders() =>
    Models.Where(model => model.TypeDiscriminator == nameof(SenderModelCard)).Cast<SenderModelCard>().ToList();

  public List<ReceiverModelCard> GetReceivers() =>
    Models.Where(model => model.TypeDiscriminator == nameof(ReceiverModelCard)).Cast<ReceiverModelCard>().ToList();

  protected string Serialize()
  {
    var serialized = JsonConvert.SerializeObject(Models, s_serializerOptions);
    return serialized;
  }

  protected static List<ModelCard> Deserialize(string models)
  {
    var deserializedModels = JsonConvert.DeserializeObject<List<ModelCard>>(models, s_serializerOptions);
    return deserializedModels;
  }

  public abstract void WriteToFile();

  public abstract void ReadFromFile();
}
