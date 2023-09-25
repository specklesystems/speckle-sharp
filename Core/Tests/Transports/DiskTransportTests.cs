using NUnit.Framework;
using Speckle.Core.Transports;

namespace TestsUnit.Transports;

[TestFixture]
[TestOf(nameof(DiskTransport.DiskTransport))]
public sealed class DiskTransportTests : TransportTests
{
  protected override ITransport Sut => _diskTransport!;

  private DiskTransport.DiskTransport? _diskTransport;

  private static readonly string BasePath = $"./temp {Guid.NewGuid()}";
  private const string ApplicationName = "Speckle Integration Tests";
  private static readonly string FullPath = Path.Combine(BasePath, ApplicationName);

  [SetUp]
  public void Setup()
  {
    _diskTransport = new DiskTransport.DiskTransport(FullPath);
  }

  [TearDown]
  public void TearDown()
  {
    Directory.Delete(BasePath, true);
  }

  [Test]
  public void DirectoryCreated_AfterInitialization()
  {
    bool fileExists = Directory.Exists(FullPath);
    Assert.That(fileExists, Is.True);
  }
}
