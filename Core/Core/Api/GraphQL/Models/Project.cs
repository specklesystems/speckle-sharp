using System;
using System.Collections.Generic;
using Speckle.Core.Api.GraphQL.Enums;

namespace Speckle.Core.Api.GraphQL.Models;

public sealed class Project
{
  public bool AllowPublicComments { get; set; }
  public ProjectCommentCollection commentThreads { get; set; }
  public DateTime createdAt { get; set; }
  public string? description { get; set; }
  public string id { get; set; }
  public List<PendingStreamCollaborator>? invitedTeam { get; set; }
  public ResourceCollection<Model> models { get; set; }
  public string name { get; set; }
  public List<FileUpload> pendingImportedModels { get; set; }
  public string role { get; set; }
  public List<string> sourceApps { get; set; }
  public List<ProjectCollaborator> team { get; set; }
  public DateTime updatedAt { get; set; }
  public ProjectVisibility visibility { get; set; }

#nullable disable

  // The ones below, we often only need inside the client (to return)
  // Ideally these "optional ones" we want to keep internal
  // if its impossible for us to properly nullability syntax them

  internal List<ViewerResourceGroup> viewerResources { get; set; }
  internal ResourceCollection<Version> versions { get; set; }
  internal Model model { get; set; }
  internal List<ModelsTreeItem> modelChildrenTree { get; set; }
  internal ResourceCollection<ModelsTreeItem> modelsTree { get; set; }
  // public WebhookCollection webhooks { get; set; } //TODO: do we want this functionality?
}
