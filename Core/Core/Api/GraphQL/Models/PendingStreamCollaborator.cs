namespace Speckle.Core.Api.GraphQL.Models;

public sealed class PendingStreamCollaborator
{
  public string id { get; init; }
  public string inviteId { get; init; }
  public string streamId { get; init; }
  public string streamName { get; init; }
  public string title { get; init; }
  public string role { get; init; }
  public LimitedUser invitedBy { get; init; }
  public LimitedUser user { get; init; }
  public string token { get; init; }
}
