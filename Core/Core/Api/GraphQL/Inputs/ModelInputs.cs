using System.Collections.Generic;

namespace Speckle.Core.Api.GraphQL.Inputs;

public sealed record CreateModelInput(string name, string projectId, string? description);

public sealed record DeleteModelInput(string id, string projectId);

public sealed record UpdateModelInput(string id, string projectId, string? name, string? description);

public sealed record ModelVersionsFilter(IReadOnlyList<string> priorityIds, bool? priorityIdsOnly);
