using System;

namespace Speckle.Core.Api.GraphQL.Enums;

public enum ProjectVisibility
{
  Private,

  Public,

  [Obsolete("Use Public instead")]
  Unlisted,
  Workspace,
}
