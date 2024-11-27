using System.Collections.Generic;

namespace Speckle.Core.Api.GraphQL.Inputs;

internal sealed record CommentContentInput(IReadOnlyCollection<string>? blobIds, object? doc);

internal sealed record CreateCommentInput(
  CommentContentInput content,
  string projectId,
  string resourceIdString,
  string? screenshot,
  object? viewerState
);

internal sealed record EditCommentInput(CommentContentInput content, string commentId, string projectId);

internal sealed record CreateCommentReplyInput(CommentContentInput content, string threadId, string projectId);

public sealed record MarkCommentViewedInput(string commentId, string projectId);

public sealed record ArchiveCommentInput(string commentId, string projectId, bool archived = true);
