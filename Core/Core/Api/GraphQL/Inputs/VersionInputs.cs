using System.Collections.Generic;

namespace Speckle.Core.Api.GraphQL.Inputs;

public sealed record UpdateVersionInput(string versionId, string? message);

public sealed record MoveVersionsInput(string targetModelName, IReadOnlyList<string> versionIds);

public sealed record DeleteVersionsInput(IReadOnlyList<string> versionIds);
