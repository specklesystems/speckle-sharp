using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using DUI3.Filters;

namespace DUI3;
public class DocumentInfo
{
  public string Location { get; set; }
  public string Name { get; set; }
  public string Id { get; set; }
}

public class DocumentState
{
  public List<IModelCard> Models { get; set; } = new List<IModelCard>();
  
  private static JsonSerializerOptions _serializerOptions = new JsonSerializerOptions
  {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
  };

  public string Serialize()
  {
    var serialized = JsonSerializer.Serialize(this, _serializerOptions);
    return serialized;
  }

  public static DocumentState Deserialize(string state)
  {
    var docState = JsonSerializer.Deserialize<DocumentState>(state, _serializerOptions);
    return new DocumentState();
  }
}

public class TestClass
{
  public TestFilter myFilter;
}

public class TestFilter
{
  // 
}

public class SpecificFilter : TestFilter
{
  // llll
}

public interface IModelCard
{
  public string Id { get; set; }
  public string ModelId { get; set; }
  public string ProjectId { get; set; }
  public string AccountId { get; set; }
  public string LastLocalUpdate { get; set; }
  public string Type { get; set; }
}


public interface ISenderCard<TSendSettings, TSendFilter> : IModelCard 
  where TSendSettings : ISendSettings
  where TSendFilter : ISendFilter
{
  public TSendSettings Settings { get; set; }
  // public TSendFilter Filter { get; set; }
}

public interface ISendSettings
{
  // TODO; probably something on the lines of Fields { type = boolean, value = false }, note we might have concrete type deserialization problems here or need a custom converrter for them if we go this way
  public  bool MergeFaces { get; set; }
  public string DoWhatever { get; set; }
  public ISendFilter Filter { get; set; 
}

public interface ISendFilter
{
  public string Name { get; set; }
  public string Summary { get; set; }
}

// public abstract class SendFilter
// {
//   public string Name { get; set; }
//   public string Summary { get; set; }
//
//   public string Type => this.GetType().FullName;
//
//   public delegate List<string> GetObjectIds();
// }
//
// public abstract class SelectionFilter : SendFilter
// {
//   public List<string> SelectedObjectIds { get; set; } = new List<string>();
// }
//
// public abstract class ListFilter : SendFilter
// {
//   public List<string> Values { get; set; } = new List<string>();
//   public List<string> SelectedValues { get; set; } = new List<string>();
// }
//
// public class ExpiredStatus
// {
//   public bool IsExpired { get; set; }
//   public string Summary { get; set; }
// }

