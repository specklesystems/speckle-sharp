#nullable disable

namespace Speckle.Core.Api.GraphQL.Models;

public sealed class Comment
{
  public Comment parent { get; init; }
  public ResourceCollection<Comment> replies { get; init; }
  public CommentReplyAuthorCollection replyAuthors { get; init; }
}
