using Speckle.Core.Api;
using Speckle.Core.Credentials;

namespace DesktopUI2.Models;

/// <summary>
/// A simple wrapper for accounts and streams so we can always know what account a stream belogs to
/// This is different from the StreamWrapper used in GH and Dynamo
/// </summary>
public class StreamAccountWrapper
{
  public StreamAccountWrapper(Stream stream, Account account)
  {
    Stream = stream;
    Account = account;
  }

  public Stream Stream { get; set; }
  public Account Account { get; set; }
}
