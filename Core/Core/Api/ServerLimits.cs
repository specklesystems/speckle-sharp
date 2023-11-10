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
}
