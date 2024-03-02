#nullable disable

using System;

namespace Speckle.Core.Api.GraphQL.Models;

public class Activity
{
  public string actionType { get; init; }
  public string id { get; init; }
  public Info info { get; init; }
  public string message { get; init; }
  public string resourceId { get; init; }
  public string resourceType { get; init; }
  public string streamId { get; init; }
  public DateTime time { get; init; }
  public string userId { get; init; }
}
