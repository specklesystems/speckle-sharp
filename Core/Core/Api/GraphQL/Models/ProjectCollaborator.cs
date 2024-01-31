namespace Speckle.Core.Api.GraphQL.Models;

public sealed class ProjectCollaborator
{
  public string role { get; set; }
  public LimitedUser user { get; set; }
}
