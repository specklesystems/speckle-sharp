#nullable disable
using System;
using System.Collections.Generic;

namespace Speckle.Core.Api.GraphQL.Models;

public abstract class UserBase
{
  public ResourceCollection<Activity> activity { get; init; }
  public string avatar { get; init; }
  public string bio { get; init; }
  public string company { get; set; }
  public string id { get; init; }
  public string name { get; init; }
  public string role { get; init; }

  public ResourceCollection<Activity> timeline { get; init; }
  public int totalOwnedStreamsFavorites { get; init; }
  public bool? verified { get; init; }

  [Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE)]
  public ResourceCollection<Commit> commits { get; init; }

  [Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE)]
  public ResourceCollection<Stream> streams { get; init; }
}

public sealed class LimitedUser : UserBase
{
  public override string ToString()
  {
    return $"Other user profile: ({name} | {id})";
  }
}

public sealed class User : UserBase
{
  public DateTime? createdAt { get; init; }
  public string email { get; init; }
  public bool? hasPendingVerification { get; init; }
  public bool? isOnboardingFinished { get; init; }
  public List<PendingStreamCollaborator> projectInvites { get; init; }
  public ResourceCollection<Project> projects { get; init; }

  [Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE)]
  public ResourceCollection<Stream> favoriteStreams { get; init; }

  public override string ToString()
  {
    return $"User ({email} | {name} | {id})";
  }
}
