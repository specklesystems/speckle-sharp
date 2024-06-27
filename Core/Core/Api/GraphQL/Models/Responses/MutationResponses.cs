namespace Speckle.Core.Api.GraphQL.Models.Responses;

#nullable disable
internal sealed class ProjectMutation
{
  public Project create { get; init; }
  public Project update { get; init; }
  public bool delete { get; init; }
  public ProjectInviteMutation invites { get; init; }

  public Project updateRole { get; init; }
}

internal sealed class ProjectInviteMutation
{
  public Project create { get; init; }
  public bool use { get; init; }
  public Project cancel { get; init; }
}

internal sealed class ModelMutation
{
  public Model create { get; init; }
  public Model update { get; init; }
  public bool delete { get; init; }
}
