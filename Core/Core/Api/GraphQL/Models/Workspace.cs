﻿using Speckle.Core.Logging;

namespace Speckle.Core.Api.GraphQL.Models;

public sealed class Workspace
{
  public string id { get; init; }
  public string name { get; init; }
  public string role { get; init; }
  public string slug { get; init; }
  public string? description { get; init; }
  public WorkspacePermissionChecks permissions { get; init; }
}

public sealed class WorkspacePermissionChecks
{
  public PermissionCheckResult canCreateProject { get; init; }
}

public sealed class PermissionCheckResult
{
  public bool authorized { get; init; }
  public string code { get; init; }
  public string message { get; init; }

  /// <exception cref="SpeckleException">Throws when <see cref="PermissionCheckResult.authorized"/> is <see langword="false"/></exception>
  public void EnsureAuthorised()
  {
    if (!authorized)
    {
      throw new SpeckleException(message);
    }
  }
}
