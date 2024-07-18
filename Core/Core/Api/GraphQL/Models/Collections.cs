using System.Collections.Generic;

namespace Speckle.Core.Api.GraphQL.Models;

public class ResourceCollection<T>
{
  public int totalCount { get; init; }

  public List<T> items { get; init; }

  public string? cursor { get; init; }
}

public sealed class CommentReplyAuthorCollection : ResourceCollection<LimitedUser> { }

public sealed class ProjectCommentCollection : ResourceCollection<Comment>
{
  public int totalArchivedCount { get; init; }
}
