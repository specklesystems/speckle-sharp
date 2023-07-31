using System;
using System.Linq;
using JetBrains.Annotations;
using System.Collections.Generic;
using DUI3.Utils;
using Speckle.Core.Api;
using Speckle.Newtonsoft.Json;
using Speckle.Newtonsoft.Json.Linq;
using Speckle.Newtonsoft.Json.Serialization;

namespace DUI3;
public class DocumentInfo
{
  public string Location { get; set; }
  public string Name { get; set; }
  public string Id { get; set; }
}

public class DocumentState
{
  public List<ModelCard> Models { get; set; } = new List<ModelCard>();

  private static JsonSerializerSettings _serializerOptions = DUI3.Utils.Serialization.GetSerializerSettings();

  public string Serialize()
  {
    var serialized = JsonConvert.SerializeObject(this, _serializerOptions);
    return serialized;
  }

  public static DocumentState Deserialize(string state)
  {
    var docState = JsonConvert.DeserializeObject(state, _serializerOptions);
    return new DocumentState();
  }
}

public class ModelCard
{
  public string Id { get; set; }
  public string ModelId { get; set; }
  public string ProjectId { get; set; }
  public string AccountId { get; set; }
  public string LastLocalUpdate { get; set; }
  public string Type { get; set; }
}


public class SenderModelCard : ModelCard
{
  public SendSettings SendSettings { get; set; }
  public new string Type { get; set; } = "sender";
}

public abstract class SendFilter : DiscriminatedObject
{
  public string Name { get; set; }
  public string Summary { get; set; }
  public abstract List<string> GetObjectIds();
}

public abstract class SendSettings : DiscriminatedObject
{
  public SendFilter Filter { get; set; }
}
