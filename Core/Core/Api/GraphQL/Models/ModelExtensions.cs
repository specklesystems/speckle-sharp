using System;

namespace Speckle.Core.Api.GraphQL.Models;

public static class ModelExtensions
{
  /// <inheritdoc cref="CanReceive(Project)"/>
  [Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE)]
  public static bool CanReceive(this Stream project) =>
    project.role is StreamRoles.STREAM_OWNER or StreamRoles.STREAM_CONTRIBUTOR;

  /// <remarks>
  /// You should prefer using <see cref="Speckle.Core.Api.GraphQL.Resources.ProjectResource.GetPermissions"/>
  /// since this server should be the source of truth for permission checks,
  /// and the logic on the serer takes into account workspace admins who should be able to receive
  /// despite not having an explicit role on a project
  /// </remarks>
  /// <param name="project"></param>
  /// <returns><see langword="True"/> if the <see cref="Stream.role"/> allows for receive</returns>
  public static bool CanReceive(this Project project) =>
    project.role is StreamRoles.STREAM_OWNER or StreamRoles.STREAM_CONTRIBUTOR;
}
