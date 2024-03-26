using NUnit.Framework;
using Serilog.Context;
using Serilog.Core;
using Serilog.Events;
using Speckle.Core.Logging;

namespace Speckle.Core.Tests.Unit.Logging;

[TestOf(typeof(SpeckleLog))]
public class SpeckleLogTests : IDisposable
{
  private StringWriter _stdOut;

  [SetUp]
  public virtual void Setup()
  {
    _stdOut = new StringWriter();
    Console.SetOut(_stdOut);
  }

  [TearDown]
  public void TearDown()
  {
    _stdOut?.Dispose();
  }

  [OneTimeTearDown]
  public void OneTimeTearDown()
  {
    var standardOutput = new StreamWriter(Console.OpenStandardOutput());
    standardOutput.AutoFlush = true;
    Console.SetOut(standardOutput);
  }

  [Test]
  [TestCase(LogEventLevel.Fatal, true)]
  [TestCase(LogEventLevel.Error, true)]
  [TestCase(LogEventLevel.Warning, true)]
  [TestCase(LogEventLevel.Information, true)]
  [TestCase(LogEventLevel.Debug, true)]
  [TestCase(LogEventLevel.Verbose, false)]
  public void LoggerWrites_WithLogEventLevel(LogEventLevel logLevel, bool expectLog)
  {
    const string TEMPLATE = "My log message";

    SpeckleLog.Logger.Write(logLevel, TEMPLATE);

    string result = _stdOut.ToString();

    if (expectLog)
    {
      Assert.That(result, Contains.Substring(TEMPLATE));
    }
    else
    {
      Assert.That(result, Is.Empty);
    }
  }

  [Test]
  public void LoggerWrites_PositionalProperties()
  {
    const string PROP_NAME = "myProp";
    const string TEMPLATE = $"My log message with positional prop {{{PROP_NAME}}}";
    const string TARGET_VALUE = "my amazing value";
    SpeckleLog.Logger.Warning(TEMPLATE, TARGET_VALUE);

    string result = _stdOut.ToString();
    Assert.That(result, Does.Contain(TARGET_VALUE));
    Assert.That(result, Does.Not.Contain(PROP_NAME));
  }

  [Test]
  public void LoggerWrites_ContextProperties()
  {
    const string PROP_NAME = "myProp";
    const string TEMPLATE = $"My log message with context prop {{{PROP_NAME}}}";
    const string TARGET_VALUE = "my amazing value";

    SpeckleLog.Logger.ForContext(PROP_NAME, TARGET_VALUE).Warning(TEMPLATE);

    string result = _stdOut.ToString();
    Assert.That(result, Does.Contain(TARGET_VALUE));
    Assert.That(result, Does.Not.Contain(PROP_NAME));
  }

  [Test]
  public void LoggerWrites_ScopedProperties()
  {
    const string PROP_NAME = "myProp";
    const string TEMPLATE = $"My log message with scoped prop {{{PROP_NAME}}}";
    const string TARGET_VALUE = "my amazing value";

    using var d1 = LogContext.PushProperty(PROP_NAME, TARGET_VALUE);

    SpeckleLog.Logger.Warning(TEMPLATE);

    string result = _stdOut.ToString();
    Assert.That(result, Does.Contain(TARGET_VALUE));
    Assert.That(result, Does.Not.Contain(PROP_NAME));
  }

  [Test]
  [TestCase(true)]
  [TestCase(false)]
  public void CreateConfiguredLogger_WritesToConsole_ToConsole(bool shouldWrite)
  {
    const string TEST_MESSAGE = "This is my test message";

    SpeckleLogConfiguration config =
      new(logToConsole: shouldWrite, logToSeq: false, logToSentry: false, logToFile: false);
    using var logger = SpeckleLog.CreateConfiguredLogger("My Test Host App!!", null, config);

    logger.Fatal(TEST_MESSAGE);

    string result = _stdOut.ToString();
    if (shouldWrite)
    {
      Assert.That(result, Does.Contain(TEST_MESSAGE));
    }
    else
    {
      Assert.That(result, Is.Empty);
    }
  }

  public void Dispose()
  {
    _stdOut?.Dispose();
  }
}
