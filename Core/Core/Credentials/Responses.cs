using System;
using System.Runtime.InteropServices;
using Speckle.Core.Api;
using Speckle.Core.Api.GraphQL.Models;

namespace Speckle.Core.Credentials;

internal sealed class ActiveUserServerInfoResponse
{
  public UserInfo activeUser { get; init; }
  public ServerInfo serverInfo { get; init; }
}

internal sealed class TokenExchangeResponse
{
  public string token { get; init; }
  public string refreshToken { get; init; }
}

[ClassInterface(ClassInterfaceType.AutoDual)]
[ComVisible(true)]
public sealed class UserInfo
{
  public string id { get; init; }
  public string name { get; init; }
  public string email { get; init; }
  public string? company { get; init; }
  public string? avatar { get; init; }

  [Obsolete(DeprecationMessages.FE2_DEPRECATION_MESSAGE)]
  public Streams streams { get; init; }

  [Obsolete(DeprecationMessages.FE2_DEPRECATION_MESSAGE)]
  public Commits commits { get; init; }
}

[ClassInterface(ClassInterfaceType.AutoDual)]
[ComVisible(true)]
[Obsolete(DeprecationMessages.FE2_DEPRECATION_MESSAGE)]
public class Streams
{
  public int totalCount { get; set; }
}

[ClassInterface(ClassInterfaceType.AutoDual)]
[ComVisible(true)]
[Obsolete(DeprecationMessages.FE2_DEPRECATION_MESSAGE)]
public class Commits
{
  public int totalCount { get; set; }
}
