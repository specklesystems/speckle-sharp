#nullable disable

using System;

namespace Speckle.Core.Api.GraphQL.Models;

public sealed class Version
{
  public LimitedUser authorUser { get; init; }

  //public AutomationStatus? automationStatus { get; set; } //See automate SDK
  public ResourceCollection<Comment> commentThreads { get; init; }
  public DateTime createdAt { get; init; }
  public string id { get; init; }
  public string message { get; init; }
  public Model model { get; init; }
  public Uri previewUrl { get; init; } //HACK: not URI type in schema
  public string referencedObject { get; init; }
  public string sourceApplication { get; init; }
}
