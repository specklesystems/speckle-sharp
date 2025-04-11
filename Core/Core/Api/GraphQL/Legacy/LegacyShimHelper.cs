using System;
using System.Linq;
using Speckle.Core.Api.GraphQL.Enums;
using Speckle.Core.Api.GraphQL.Models;

namespace Speckle.Core.Api.GraphQL.Legacy;

[Obsolete("Don't rely on this")]
public static class LegacyShimHelper
{
  /// <summary>
  /// Shimms a project data into a stream data for legacy codebases where a mix of old and new APIs are used
  /// Does not copy branches and commits etc over
  /// </summary>
  /// <param name="project"></param>
  /// <returns></returns>
  public static Stream ToLegacy(this Project current)
  {
    return new Stream()
    {
      id = current.id,
      name = current.name,
      description = current.description,
      isPublic = current.visibility != ProjectVisibility.Private,
      role = current.role,
      createdAt = current.createdAt,
      updatedAt = current.updatedAt,
      commentCount = current.commentThreads.totalCount,
      collaborators = current.team?.Select(ToLegacy).ToList(),
      pendingCollaborators = current.invitedTeam,
    };
  }

  [Obsolete("Don't rely on this")]
  public static Collaborator ToLegacy(this ProjectCollaborator current)
  {
    return new Collaborator()
    {
      avatar = current.user.avatar,
      id = current.id,
      name = current.user.name,
      role = current.role
    };
  }
}
