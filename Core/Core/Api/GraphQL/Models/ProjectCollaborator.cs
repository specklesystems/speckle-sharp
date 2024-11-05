#nullable disable

namespace Speckle.Core.Api.GraphQL.Models;

public sealed class ProjectCollaborator
{
  public string id { get; init; }
  public string role { get; init; }
  public LimitedUser user { get; init; }
}
