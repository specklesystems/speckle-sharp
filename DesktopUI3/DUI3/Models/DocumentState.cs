using System.Collections.Generic;
using DUI3.Utils;
using Speckle.Newtonsoft.Json;

namespace DUI3.Models;

public class DocumentState
{
  public List<ModelCard> Models { get; set; } = new List<ModelCard>();

  private static readonly JsonSerializerSettings SerializerOptions = DUI3.Utils.SerializationSettingsFactory.GetSerializerSettings();

  public string Serialize()
  {
    var serialized = JsonConvert.SerializeObject(this, SerializerOptions);
    return serialized;
  }

  public static DocumentState Deserialize(string state)
  {
    var docState = JsonConvert.DeserializeObject(state, SerializerOptions);
    return new DocumentState();
  }
}


