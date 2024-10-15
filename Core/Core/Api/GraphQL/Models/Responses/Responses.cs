using Speckle.Newtonsoft.Json;

namespace Speckle.Core.Api.GraphQL.Models.Responses;

// This file holds simple records that represent the root GraphQL response data
// For this reason, we're keeping them internal, allowing us to be flexible without the concern for breaking.
// It also exposes fewer similarly named types to dependent assemblies

internal record ProjectResponse([property: JsonRequired] Project project);

internal record ActiveUserResponse(User? activeUser);

internal record LimitedUserResponse(LimitedUser? otherUser);

internal record ServerInfoResponse([property: JsonRequired] ServerInfo serverInfo);

internal record ProjectMutationResponse([property: JsonRequired] ProjectMutation projectMutations);

internal record ModelMutationResponse([property: JsonRequired] ModelMutation modelMutations);

internal record VersionMutationResponse([property: JsonRequired] VersionMutation versionMutations);

internal record ProjectInviteResponse(PendingStreamCollaborator? projectInvite);

internal record UserSearchResponse([property: JsonRequired] ResourceCollection<LimitedUser> userSearch);

//All of the above records could be replaced by either RequiredResponse or OptionalResponse, if we use an alias (see https://www.baeldung.com/graphql-field-name)
internal record RequiredResponse<T>([property: JsonRequired] T data);

internal record OptionalResponse<T>(T? data);
