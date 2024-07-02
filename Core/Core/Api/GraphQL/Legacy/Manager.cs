#nullable disable
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Speckle.Core.Api.GraphQL;

#region manager api

public class Connector
{
  public List<ConnectorVersion> Versions { get; set; } = new();
}

public class ConnectorVersion
{
  public ConnectorVersion(string number, string url, Os os = Os.Win, Architecture architecture = Architecture.Any)
  {
    Number = number;
    Url = url;
    Date = DateTime.Now;
    Prerelease = Number.Contains("-");
    Os = os;
    Architecture = architecture;
  }

  public string Number { get; set; }
  public string Url { get; set; }
  public Os Os { get; set; }
  public Architecture Architecture { get; set; } = Architecture.Any;
  public DateTime Date { get; set; }

  [JsonIgnore]
  public string DateTimeAgo => Helpers.TimeAgo(Date);

  public bool Prerelease { get; set; }
}

/// <summary>
/// OS
/// NOTE: do not edit order and only append new items as they are serialized to ints
/// </summary>
public enum Os
{
  Win, //0
  OSX, //1
  Linux, //2
  Any //3
}

/// <summary>
/// Architecture
/// NOTE: do not edit order and only append new items as they are serialized to ints
/// </summary>
public enum Architecture
{
  Any, //0
  Arm, //1
  Intel //2
}

//GHOST API
public class Meta
{
  public Pagination pagination { get; set; }
}

public class Pagination
{
  public int page { get; set; }
  public string limit { get; set; }
  public int pages { get; set; }
  public int total { get; set; }
  public object next { get; set; }
  public object prev { get; set; }
}

public class Tags
{
  public List<Tag> tags { get; set; }
  public Meta meta { get; set; }
}

public class Tag
{
  public string id { get; set; }
  public string name { get; set; }
  public string slug { get; set; }
  public string description { get; set; }
  public string feature_image { get; set; }
  public string visibility { get; set; }
  public string codeinjection_head { get; set; }
  public object codeinjection_foot { get; set; }
  public object canonical_url { get; set; }
  public string accent_color { get; set; }
  public string url { get; set; }
}
#endregion
