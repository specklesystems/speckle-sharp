#nullable disable

using System;

namespace Speckle.Core.Api.GraphQL.Models;

public sealed class Version
{
  public LimitedUser authorUser { get; init; }
  public ResourceCollection<Comment> commentThreads { get; init; }
  public DateTime createdAt { get; init; }
  public string id { get; init; }
  public string message { get; init; }
  public Model model { get; init; }
  public Uri previewUrl { get; init; }
  public string referencedObject { get; init; }
  public string sourceApplication { get; init; }
}
