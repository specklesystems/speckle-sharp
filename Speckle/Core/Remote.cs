using System;
using Speckle.Transports;

namespace Speckle.Core
{
  public class Remote
  {
    public Account Account { get; set; }

    public string Name { get; set; }

    public Stream Stream { get; set; }

    public Remote() { }

    public Remote(Account account, string StreamId, string name)
    {
      this.Account = account;
      this.Name = name;
    }

    public ITransport GetTransport()
    {
      return new MemoryTransport() { TransportName = $"Remote {Name} (MOCK)" };
    }
  }
}
