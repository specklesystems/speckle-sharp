using Speckle.Newtonsoft.Json;

namespace Speckle.Core.Api.GraphQL.Models;

// This file holds simple structs that represent the root GraphQL response data
// They need not be exhaustive, for some return types, we've chosen to use a Dictionary<string, T> instead
// For this reason, we're keeping them internal, allowing us to be flexible without the concern for breaking.
// TODO: readonly struct VS class, any benefit?
// TODO: We may not beable to make them internal. SpeckleGraphQLException<T> expects T to be the response type. User won't be able to catch these exceptions
// TODO: All of these structs could be replaced by this DataResponse, if we use an alias (see https://www.baeldung.com/graphql-field-name)
//internal readonly record struct DataResponse<T>([property: JsonRequired] T data);

internal readonly record struct ProjectData([property: JsonRequired] Project project);

internal readonly record struct ActiveUserData([property: JsonRequired] User activeUser);

internal readonly record struct LimitedUserData([property: JsonRequired] LimitedUser otherUser);

internal readonly record struct ServerInfoResponse([property: JsonRequired] ServerInfo serverInfo);

internal readonly record struct ProjectMutationResponse([property: JsonRequired] ProjectMutation projectMutations);

#nullable disable
public class ProjectMutation
{
  public Project create { get; init; }
  public Project update { get; init; }
  public bool delete { get; init; }
  public ProjectInviteMutation invites { get; init; }
}

public class ProjectInviteMutation
{
  public Project create { get; init; }
  public Project use { get; init; }
}
#nullable enable
