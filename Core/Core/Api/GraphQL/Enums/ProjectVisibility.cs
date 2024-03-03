namespace Speckle.Core.Api.GraphQL.Enums;

public enum ProjectVisibility
{
  //TODO: Check how the server stores values of this type, are they ints, or strings (if so, we may have problems)
  Private,
  Public,
  Unlisted
}
