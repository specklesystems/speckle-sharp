namespace Speckle.Core.Api.GraphQL;

/// <summary>
/// These are the default roles used by the server
/// </summary>
public static class StreamRoles
{
  public const string STREAM_OWNER = "stream:owner";
  public const string STREAM_CONTRIBUTOR = "stream:contributor";
  public const string STREAM_REVIEWER = "stream:reviewer";
  public const string? REVOKE = null;
}
