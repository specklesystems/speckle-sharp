using System;
using System.Collections.Generic;
using System.Linq;
using DUI3.Bindings;
using DUI3.Utils;
using JetBrains.Annotations;
using Speckle.Newtonsoft.Json;

namespace DUI3.Models;

/// <summary>
/// Encapsulates the state Speckle needs to persist in the host app's document. 
/// </summary>
public abstract class DocumentModelStore : DiscriminatedObject
{
  public List<ModelCard> Models { get; set; } = new List<ModelCard>();

  private static readonly JsonSerializerSettings SerializerOptions = DUI3.Utils.SerializationSettingsFactory.GetSerializerSettings();
  
  /// <summary>
  /// This event is triggered by each specific host app implementation of the document model store.
  /// </summary>
  [PublicAPI]
  public event EventHandler DocumentChanged;

  public virtual bool IsDocumentInit { get; set; } = false;
  public ModelCard GetModelById(string id)
  {
    var model = Models.First(model => model.Id == id);
    return model;
  }

  protected void OnDocumentChanged()
  {
    var handler = DocumentChanged;
    if (handler != null)
    {
      handler(this, EventArgs.Empty);
    }
  }
  
  public List<SenderModelCard> GetSenders()
  {
    return Models.Where(model => model.TypeDiscriminator == nameof(SenderModelCard)).Cast<SenderModelCard>().ToList();
  }
  
  public List<SenderModelCard> GetReceivers()
  {
    // return Models.Where(model => model.TypeDiscriminator == nameof(SenderModelCard)).Cast<ReceiverModel>().ToList();
    throw new NotImplementedException();
  }

  protected string Serialize()
  {
    var serialized = JsonConvert.SerializeObject(Models, SerializerOptions);
    return serialized;
  }

  protected static List<ModelCard> Deserialize(string models)
  {
    var deserializedModels = JsonConvert.DeserializeObject<List<ModelCard>>(models, SerializerOptions);
    return deserializedModels;
  }

  public abstract void WriteToFile();
  
  public abstract void ReadFromFile(); 
}


