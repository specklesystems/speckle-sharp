namespace Speckle.Core.Api.GraphQL.Models;

public sealed class Comment
{
  public Comment? parent { get; set; }
  public ResourceCollection<Comment> replies { get; set; }
  public CommentReplyAuthorCollection replyAuthors { get; set; }
}
