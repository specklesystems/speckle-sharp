#nullable disable
using Speckle.Core.Api.GraphQL.Enums;

namespace Speckle.Core.Api.GraphQL.Models;

public sealed class ResourceIdentifier
{
  public string resourceId { get; init; }
  public ResourceType resourceType { get; init; }
}
