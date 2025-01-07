#nullable disable
using System;
using System.Collections.Generic;

namespace Speckle.Core.Api.GraphQL.Models;

public sealed class Model
{
  public LimitedUser author { get; init; }
  public List<ModelsTreeItem> childrenTree { get; init; }
  public ResourceCollection<Comment> commentThreads { get; init; }
  public DateTime createdAt { get; init; }
  public string description { get; init; }
  public string displayName { get; init; }
  public string id { get; init; }
  public string name { get; init; }
  public List<FileUpload> pendingImportedVersions { get; init; }
  public Uri previewUrl { get; init; }
  public DateTime updatedAt { get; init; }
  public ResourceCollection<Version> versions { get; init; }
  public Version version { get; init; }
}
