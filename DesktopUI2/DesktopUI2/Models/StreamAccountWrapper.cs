using Speckle.Core.Api;
using Speckle.Core.Credentials;

namespace DesktopUI2.Models
{
  public class StreamAccountWrapper
  {
    public Stream Stream { get; set; }
    public Account Account { get; set; }

    public StreamAccountWrapper(Stream stream, Account account)
    {
      Stream = stream;
      Account = account;
    }
  }
}
