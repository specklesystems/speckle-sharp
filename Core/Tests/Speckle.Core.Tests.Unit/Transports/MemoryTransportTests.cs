using System.Collections.Concurrent;
using NUnit.Framework;
using Speckle.Core.Transports;

namespace Speckle.Core.Tests.Unit.Transports;

[TestFixture]
[TestOf(nameof(MemoryTransport))]
public sealed class MemoryTransportTests : TransportTests
{
  protected override ITransport Sut => _memoryTransport!;

  private MemoryTransport _memoryTransport;

  [SetUp]
  public void Setup()
  {
    _memoryTransport = new MemoryTransport(new ConcurrentDictionary<string, string>());
  }
}
