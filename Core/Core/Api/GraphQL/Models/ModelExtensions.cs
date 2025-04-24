using System;

namespace Speckle.Core.Api.GraphQL.Models;

public static class ModelExtensions
{
  /// <inheritdoc cref="CanReceive(Project)"/>
  [Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE)]
  public static bool CanReceive(this Stream project) =>
    project.role is StreamRoles.STREAM_OWNER or StreamRoles.STREAM_CONTRIBUTOR;

  /// <param name="project"></param>
  /// <returns><see langword="True"/> if the <see cref="Stream.role"/> allows for receive</returns>
  public static bool CanReceive(this Project project) =>
    project.role is StreamRoles.STREAM_OWNER or StreamRoles.STREAM_CONTRIBUTOR;
}
