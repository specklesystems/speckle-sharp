#nullable disable

using System;
using System.Collections.Generic;
using Speckle.Newtonsoft.Json;

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
  public ViewerState viewerState { get; init; }
}

/// <summary>
/// See <c>SerializedViewerState</c> in <a href="https://github.com/specklesystems/speckle-server/blob/main/packages/shared/src/viewer/helpers/state.ts">/shared/src/viewer/helpers/state.ts</a>
/// </summary>
/// <remarks>
/// Note, there are many FE/Viewer specific properties on this object that are not reflected here (hence the <see cref="MissingMemberHandling"/> override)
/// We can add them as needed, keeping in mind flexiblity for breaking changes (these classes are intentionally not documented in our schema!)
/// </remarks>
[JsonObject(MissingMemberHandling = MissingMemberHandling.Ignore)]
public sealed class ViewerState
{
  public ViewerStateUI ui { get; init; }
}

[JsonObject(MissingMemberHandling = MissingMemberHandling.Ignore)]
public sealed class ViewerStateUI
{
  public ViewerStateCamera camera { get; init; }
}

[JsonObject(MissingMemberHandling = MissingMemberHandling.Ignore)]
public sealed class ViewerStateCamera
{
  public List<double> position { get; init; }
  public List<double> target { get; init; }
  public bool isOrthoProjection { get; init; }
  public double zoom { get; init; }
}
