using NUnit.Framework;
using Speckle.Core.Logging;

namespace Objects.Tests
{
  [SetUpFixture]
  public class NUnitFixtures
  {
    [OneTimeSetUp]
    public void RunBeforeAnyTests()
    {
      SpeckleLog.Initialize(
        "ObjectsTests",
        "Testing",
        new SpeckleLogConfiguration(
          Serilog.Events.LogEventLevel.Debug,
          logToConsole: false,
          logToFile: false,
          logToSeq: false
        )
      );
      SpeckleLog.Logger.Information("Initialized logger for testing");
    }


    [OneTimeTearDown]
    public void RunAfterAnyTests()
    {
      
    }
  }
}
