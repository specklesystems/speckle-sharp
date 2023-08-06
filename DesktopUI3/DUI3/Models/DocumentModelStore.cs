using System;
using System.Collections.Generic;
using System.Linq;
using DUI3.Bindings;
using DUI3.Utils;
using Speckle.Newtonsoft.Json;

namespace DUI3.Models;

public class DocumentModelStore
{
  public List<ModelCard> Models { get; set; } = new List<ModelCard>();

  private static readonly JsonSerializerSettings SerializerOptions = DUI3.Utils.SerializationSettingsFactory.GetSerializerSettings();

  public ModelCard GetModelById(string modelId)
  {
    var model = Models.First(model => model.ModelId == modelId);
    return model;
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
  
  public string Serialize()
  {
    var serialized = JsonConvert.SerializeObject(this, SerializerOptions);
    return serialized;
  }

  public static DocumentModelStore Deserialize(string state)
  {
    var docState = JsonConvert.DeserializeObject<DocumentModelStore>(state, SerializerOptions);
    return docState;
  }
}


