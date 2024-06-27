#nullable disable
using System;

namespace Speckle.Core.Api.GraphQL.Models;

public sealed class PendingStreamCollaborator
{
  public string id { get; init; }
  public string inviteId { get; init; }

  public string projectId { get; init; }

  public string projectName { get; init; }
  public string title { get; init; }
  public string role { get; init; }
  public LimitedUser invitedBy { get; init; }
  public LimitedUser user { get; init; }
  public string token { get; init; }

  [Obsolete(DeprecationMessages.FE2_DEPRECATION_MESSAGE)]
  public string streamId { get; init; }

  [Obsolete(DeprecationMessages.FE2_DEPRECATION_MESSAGE)]
  public string streamName { get; init; }
}
