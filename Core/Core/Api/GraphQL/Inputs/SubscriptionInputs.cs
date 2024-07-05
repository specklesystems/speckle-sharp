namespace Speckle.Core.Api.GraphQL.Inputs;

public sealed record ViewerUpdateTrackingTarget(
  string projectId,
  string resourceIdString,
  bool? loadedVersionsOnly = null
);
