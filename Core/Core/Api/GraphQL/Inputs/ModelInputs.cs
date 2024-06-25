using System.Collections.Generic;
using Speckle.Newtonsoft.Json;

namespace Speckle.Core.Api.GraphQL.Inputs;

public sealed record CreateModelInput(string name, string? description, string projectId);

public sealed record DeleteModelInput(string id, string projectId);

public sealed record UpdateModelInput(string id, string? name, string? description, string projectId);

public sealed record ModelVersionsFilter(IReadOnlyList<string> priorityIds, bool? priorityIdsOnly);
