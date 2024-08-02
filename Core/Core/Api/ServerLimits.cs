namespace Speckle.Core.Api;

/// <summary>
/// Defines the limits for specific API calls on the Speckle Server.
/// These are magic numbers! Should be aligned with server always.
/// </summary>
/// <remarks>
/// ⚠️ Not all limits are reflected here!
/// </remarks>
public static class ServerLimits
{
  public const int BRANCH_GET_LIMIT = 500;
  public const int OLD_BRANCH_GET_LIMIT = 100;

  /// <summary>the default `limit` argument value for paginated requests</summary>
  public const int DEFAULT_PAGINATION_REQUEST = 25;
}
