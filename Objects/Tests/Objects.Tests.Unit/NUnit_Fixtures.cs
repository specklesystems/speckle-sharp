using NUnit.Framework;
using Speckle.Core.Logging;

namespace Objects.Tests.Unit;

[SetUpFixture]
public class NUnitFixtures
{
  [OneTimeSetUp]
  public void RunBeforeAnyTests()
  {
    SpeckleLog.Initialize(
      "ObjectsTests",
      "Testing",
      new SpeckleLogConfiguration(logToConsole: true, logToFile: true, logToSeq: true)
    );
    SpeckleLog.Logger.Information("Initialized logger for testing");
  }

  [OneTimeTearDown]
  public void RunAfterAnyTests() { }
}
