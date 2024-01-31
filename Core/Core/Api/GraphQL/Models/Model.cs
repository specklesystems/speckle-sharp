using System;
using System.Collections.Generic;

namespace Speckle.Core.Api.GraphQL.Models;

public sealed class Model
{
  public LimitedUser author { get; set; }

  // public AutomationStatus? automationStatus { get; set; } //See automate SDK
  public List<ModelsTreeItem> childrenTree { get; set; }
  public ResourceCollection<Comment> commentThreads { get; set; }
  public DateTime createdAt { get; set; }
  public string? description { get; set; }
  public string displayName { get; set; }
  public string id { get; set; }
  public string name { get; set; }
  public List<FileUpload> pendingImportedVersions { get; set; }
  public Uri? previewUrl { get; set; } //HACK: not URI type in schema
  public DateTime updatedAt { get; set; }
  public ResourceCollection<Version> versions { get; set; }

#nullable disable
  internal Version version { get; set; }
}
