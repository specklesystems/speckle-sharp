#nullable disable

using System;
using System.Collections.Generic;

namespace Speckle.Core.Api.GraphQL.Models;

public sealed class Comment
{
  public bool archived { get; init; }
  public LimitedUser author { get; init; }
  public string authorId { get; init; }
  public DateTime createdAt { get; init; }
  public bool hasParent { get; init; }
  public string id { get; init; }
  public Comment parent { get; init; }
  public string rawText { get; init; }
  public ResourceCollection<Comment> replies { get; init; }
  public CommentReplyAuthorCollection replyAuthors { get; init; }
  public List<ResourceIdentifier> resources { get; init; }
  public string screenshot { get; init; }
  public DateTime updatedAt { get; init; }
  public DateTime? viewedAt { get; init; }
  public List<ViewerResourceItem> viewerResources { get; init; }
}
