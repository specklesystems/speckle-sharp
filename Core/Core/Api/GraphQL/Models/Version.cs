using System;

namespace Speckle.Core.Api.GraphQL.Models;

public sealed class Version
{
  public LimitedUser? authorUser { get; set; }

  //public AutomationStatus? automationStatus { get; set; } //See automate SDK
  public ResourceCollection<Comment> commentThreads { get; set; }
  public DateTime createdAt { get; set; }
  public string id { get; set; }
  public string? message { get; set; }
  public required Model model { get; set; }
  public Uri previewUrl { get; set; } //HACK: not URI type in schema
  public string referencedObject { get; set; }
  public string? sourceApplication { get; set; }
}
