using NUnit.Framework;
using Speckle.Core.Credentials;
using Speckle.Core.Logging;

namespace TestsUnit;

[TestFixture]
public class FE2WrapperTests
{
  [TestCase("https://latest.speckle.systems/projects/92b620fb17/models/76fd8a01c8", "92b620fb17", "76fd8a01c8")]
  [TestCase(
    "https://latest.speckle.systems/projects/92b620fb17/models/76fd8a01c8@7dc324e4bb",
    "92b620fb17",
    "76fd8a01c8",
    "7dc324e4bb"
  )]
  public void ParseFE2Links(
    string url,
    string expectedProjectId,
    string expectedBranchId,
    string? expectedObjectId = null
  )
  {
    var streamWrapper = new StreamWrapper(url);
    Assert.NotNull(streamWrapper);
    Assert.That(streamWrapper.StreamId, Is.EqualTo(expectedProjectId));
    Assert.That(streamWrapper.BranchName, Is.EqualTo(expectedBranchId));
    Assert.That(streamWrapper.ObjectId, Is.EqualTo(expectedObjectId));
  }

  [TestCase("https://latest.speckle.systems/projects/92b620fb17/models/all")]
  [TestCase("https://latest.speckle.systems/projects/92b620fb17/models/0fe8ca21c0,76fd8a01c8")]
  [TestCase("https://latest.speckle.systems/projects/92b620fb17/models/A,76fd8a01c8@7dc324e4bb,B@C,D@E,F")]
  public void ParseFE2NotSupportedLinks(string url)
  {
    Assert.Throws<NotSupportedException>(() =>
    {
      var streamWrapper = new StreamWrapper(url);
    });
  }

  [TestCase("https://latest.speckle.systems/projects")]
  [TestCase("https://latest.speckle.systems/projects/92b620fb17")]
  [TestCase("https://latest.speckle.systems/projects/92b620fb17/models/")]
  public void ParseFE2InvalidLinks(string url)
  {
    Assert.Throws<SpeckleException>(() =>
    {
      var streamWrapper = new StreamWrapper(url);
    });
  }
}
