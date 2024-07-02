using System.Collections.Generic;
using Speckle.Core.Api.GraphQL.Enums;

namespace Speckle.Core.Api.GraphQL.Inputs;

public sealed record ProjectCommentsFilter(
  bool? includeArchived, //TODO: check sensible nullability + which ones should default null (same goes for all the inputs below)
  bool? loadedVersionsOnly, //TODO: check sensible nullability
  string? resourceIdString
);

public sealed record ProjectCreateInput(string? name, string? description, ProjectVisibility? visibility);

public sealed record ProjectInviteCreateInput(string? email, string? role, string? serverRole, string? userId);

public sealed record ProjectInviteUseInput(bool accept, string projectId, string token);

public sealed record ProjectModelsFilter(
  IReadOnlyList<string>? contributors,
  IReadOnlyList<string>? excludeIds,
  IReadOnlyList<string>? ids,
  bool? onlyWithVersions,
  string? search,
  IReadOnlyList<string> sourceApps
);

public sealed record ProjectModelsTreeFilter(
  IReadOnlyList<string>? contributors,
  string? search,
  IReadOnlyList<string>? sourceApps
);

public sealed record ProjectUpdateInput(
  string id,
  string? name = null,
  string? description = null,
  bool? allowPublicComments = null,
  ProjectVisibility? visibility = null
);

//TODO: can we enum the role?
public sealed record ProjectUpdateRoleInput(string userId, string projectId, string? role);

public sealed record UserProjectsFilter(string search, IReadOnlyList<string>? onlyWithRoles = null);
