namespace Speckle.Core.Api.GraphQL.Enums;

//This enum isn't explicitly defined in the schema, instead its usages are int typed (But represent an enum)
public enum FileUploadConversionStatus
{
  Queued,
  Processing,
  Success,
  Error,
}
