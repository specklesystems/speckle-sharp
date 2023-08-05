using System.Collections.Generic;
using DUI3.Utils;
using Speckle.Newtonsoft.Json;

namespace DUI3.Models;

public class DocumentModelStore
{
  public List<ModelCard> Models { get; set; } = new List<ModelCard>();

  private static readonly JsonSerializerSettings SerializerOptions = DUI3.Utils.SerializationSettingsFactory.GetSerializerSettings();
  
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


