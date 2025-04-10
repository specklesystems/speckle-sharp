using System;

namespace Speckle.Core.Api.GraphQL.Enums;

public enum ProjectVisibility
{
  Private,

  [Obsolete("Use Unlisted instead")]
  Public,
  Unlisted
}
