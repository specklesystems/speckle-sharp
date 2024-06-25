namespace Speckle.Core.Api.GraphQL.Models.Responses;

#nullable disable
internal class ProjectMutation
{
  public Project create { get; init; }
  public Project update { get; init; }
  public bool delete { get; init; }
  public ProjectInviteMutation invites { get; init; }
}

internal class ProjectInviteMutation
{
  public Project create { get; init; }
  public Project use { get; init; }
}

internal class ModelMutation
{
  public Model create { get; init; }
  public Model update { get; init; }
  public bool delete { get; init; }
}
