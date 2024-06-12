using NUnit.Framework;
using Speckle.Core.Credentials;
using Speckle.Core.Logging;

namespace Speckle.Core.Tests.Unit.Credentials;

[TestFixture]
[TestOf(typeof(StreamWrapper))]
public class Fe2WrapperTests
{
  [TestCase(
    "https://latest.speckle.systems/projects/92b620fb17/models/76fd8a01c8",
    StreamWrapperType.Branch,
    "92b620fb17",
    "76fd8a01c8"
  )]
  [TestCase(
    "https://latest.speckle.systems/projects/92b620fb17/models/76fd8a01c8@7dc324e4bb",
    StreamWrapperType.Commit,
    "92b620fb17",
    "76fd8a01c8",
    "7dc324e4bb"
  )]
  [TestCase(
    "https://latest.speckle.systems/projects/92b620fb17/models/bdd52d7fd174328a080770e2a7fef98a",
    StreamWrapperType.Object,
    "92b620fb17",
    null,
    null,
    "bdd52d7fd174328a080770e2a7fef98a"
  )]
  public void ParseFe2Links(
    string url,
    StreamWrapperType expectedType,
    string expectedProjectId,
    string expectedBranchId = null,
    string expectedCommitId = null,
    string expectedObjectId = null
  )
  {
    var streamWrapper = new StreamWrapper(url);
    Assert.That(streamWrapper, Is.Not.Null);
    Assert.That(streamWrapper.Type, Is.EqualTo(expectedType));
    Assert.That(streamWrapper.StreamId, Is.EqualTo(expectedProjectId));
    Assert.That(streamWrapper.BranchName, Is.EqualTo(expectedBranchId));
    Assert.That(streamWrapper.CommitId, Is.EqualTo(expectedCommitId));
    Assert.That(streamWrapper.ObjectId, Is.EqualTo(expectedObjectId));
  }

  [TestCase("https://latest.speckle.systems/projects/92b620fb17/models/all")]
  [TestCase("https://latest.speckle.systems/projects/92b620fb17/models/0fe8ca21c0,76fd8a01c8")]
  [TestCase("https://latest.speckle.systems/projects/92b620fb17/models/A,76fd8a01c8@7dc324e4bb,B@C,D@E,F")]
  public void ParseFe2NotSupportedLinks(string url)
  {
    Assert.Throws<NotSupportedException>(() => new StreamWrapper(url));
  }

  [TestCase("https://latest.speckle.systems/")]
  [TestCase("https://latest.speckle.systems/projects")]
  [TestCase("https://latest.speckle.systems/projects/")]
  [TestCase("https://latest.speckle.systems/projects/92b620fb17")]
  [TestCase("https://latest.speckle.systems/projects/92b620fb17/")]
  [TestCase("https://latest.speckle.systems/projects/92b620fb17/models/")]
  public void ParseFe2InvalidLinks(string url)
  {
    Assert.Throws<SpeckleException>(() => new StreamWrapper(url));
  }
}
