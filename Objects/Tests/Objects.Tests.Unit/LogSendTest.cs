using System.Threading.Tasks;
using NUnit.Framework;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Models;
using Speckle.Core.Transports;
using Speckle.Sdk.Logging;
using SpecklePathProvider = Speckle.Core.Helpers.SpecklePathProvider;

namespace Objects.Tests.Unit;

public class LogSendTest
{
  [Test]
  // [TestCase("5cbf84a0061172102ef8a66ae914f232", "https://bakerhicks.speckle.xyz/projects/872b5e6927/models/3a4fbb8a2a")]
  [TestCase("5cbf84a0061172102ef8a66ae914f232", "https://testing1.speckle.dev/projects/cdedc63e6d/models/2d68380f1d")]
  public async Task SendTest(string objectId, string destination)
  {
    var testData = await GetSampleData(objectId);

    SpeckleLog.Logger.Information("Starting Long Send Test Send");

    var destinationTransport = await GetDestination(destination);

    var res = await Operations.Send(testData, new[] { destinationTransport });

    SpeckleLog.Logger.Information($"Starting Send was successful: {objectId}", res);
  }

  private async Task<ITransport> GetDestination(string destination)
  {
    StreamWrapper sw = new(destination);
    var acc = await sw.GetAccount();
    return new ServerTransport(acc, sw.StreamId);
  }

  private async Task<Base> GetSampleData(string objectId)
  {
    SpeckleLog.Logger.Information("Gathering Sample Data Set");

    using SQLiteTransport source = new(SpecklePathProvider.UserApplicationDataPath(), "longsendtest");
    MemoryTransport memoryTransport = new();
    return await Operations.Receive(objectId, source, memoryTransport);
  }
}
