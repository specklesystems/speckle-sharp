using NUnit.Framework;
using Speckle.Core.Transports;

namespace Speckle.Core.Tests.Unit.Transports;

[TestFixture]
[TestOf(nameof(DiskTransport))]
public sealed class DiskTransportTests : TransportTests
{
  protected override ITransport Sut => _diskTransport!;

  private DiskTransport _diskTransport;

  private static readonly string s_basePath = $"./temp {Guid.NewGuid()}";
  private const string APPLICATION_NAME = "Speckle Integration Tests";
  private static readonly string s_fullPath = Path.Combine(s_basePath, APPLICATION_NAME);

  [SetUp]
  public void Setup()
  {
    _diskTransport = new DiskTransport(s_fullPath);
  }

  [TearDown]
  public void TearDown()
  {
    Directory.Delete(s_basePath, true);
  }

  [Test]
  public void DirectoryCreated_AfterInitialization()
  {
    bool fileExists = Directory.Exists(s_fullPath);
    Assert.That(fileExists, Is.True);
  }
}
