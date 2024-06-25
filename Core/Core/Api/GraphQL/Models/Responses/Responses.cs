using Speckle.Newtonsoft.Json;

namespace Speckle.Core.Api.GraphQL.Models.Responses;

// This file holds simple structs that represent the root GraphQL response data
// For this reason, we're keeping them internal, allowing us to be flexible without the concern for breaking.
// TODO: We may not beable to make them internal. SpeckleGraphQLException<T> expects T to be the response type. User won't be able to catch these exceptions
// TODO: All of these structs could be replaced by this DataResponse, if we use an alias (see https://www.baeldung.com/graphql-field-name)
//internal readonly record struct DataResponse<T>([property: JsonRequired] T data);

internal record ProjectResponse([property: JsonRequired] Project project);

internal record ActiveUserResponse(UserInfo? activeUser);

internal record LimitedUserResponse(LimitedUser? otherUser);

internal record ServerInfoResponse([property: JsonRequired] ServerInfo serverInfo);

internal record ProjectMutationResponse([property: JsonRequired] ProjectMutation projectMutations);

internal record ModelMutationResponse([property: JsonRequired] ModelMutation modelMutations);

internal record ProjectInviteResponse(PendingStreamCollaborator? projectInvite);
