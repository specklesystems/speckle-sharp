#nullable disable
using System;

namespace Speckle.Core.Api.GraphQL.Models;

public sealed class ServerInfo
{
  public string name { get; init; }
  public string company { get; init; }
  public string version { get; init; }
  public string adminContact { get; init; }
  public string description { get; init; }

  /// <remarks>
  /// This field is not returned from the GQL API,
  /// it was previously populated after construction from the response headers, but now FE1 is deprecated, so we should always assume FE2
  /// </remarks>
  public bool frontend2 { get; set; } = true;

  /// <remarks>
  /// This field is not returned from the GQL API,
  /// it should be populated after construction.
  /// see <see cref="Speckle.Core.Credentials.AccountManager"/>
  /// </remarks>
  public string url { get; set; }

  public ServerMigration migration { get; init; }
}

public sealed class ServerMigration
{
  /// <summary>
  /// New URI where this server is now deployed
  /// </summary>
  public Uri movedTo { get; set; }

  /// <summary>
  /// Previous URI where this server used to be deployed
  /// </summary>
  public Uri movedFrom { get; set; }
}
