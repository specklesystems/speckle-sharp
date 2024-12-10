using System.Collections.Generic;

namespace Speckle.Core.Api.GraphQL.Inputs;

public sealed record UpdateVersionInput(string versionId, string projectId, string? message);

public sealed record MoveVersionsInput(string projectId, string targetModelName, IReadOnlyList<string> versionIds);

public sealed record DeleteVersionsInput(IReadOnlyList<string> versionIds, string projectId);
