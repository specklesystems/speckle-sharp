using NUnit.Framework;
using Speckle.Core.Credentials;

namespace Tests;

[TestFixture]
public class WrapperTests
{
  [Test]
  public void ParseStream()
  {
    var wrapper = new StreamWrapper("https://testing.speckle.dev/streams/a75ab4f10f");
    Assert.That(wrapper.Type, Is.EqualTo(StreamWrapperType.Stream));
  }

  [Test]
  public void ParseBranch()
  {
    var wrapperCrazy = new StreamWrapper(
      "https://testing.speckle.dev/streams/4c3ce1459c/branches/%F0%9F%8D%95%E2%AC%85%F0%9F%8C%9F%20you%20wat%3F"
    );
    Assert.That(wrapperCrazy.BranchName, Is.EqualTo("🍕⬅🌟 you wat?"));
    Assert.That(wrapperCrazy.Type, Is.EqualTo(StreamWrapperType.Branch));

    wrapperCrazy = new StreamWrapper("https://testing.speckle.dev/streams/4c3ce1459c/branches/next%20level");
    Assert.That(wrapperCrazy.BranchName, Is.EqualTo("next level"));
    Assert.That(wrapperCrazy.Type, Is.EqualTo(StreamWrapperType.Branch));
  }

  [Test]
  public void ParseObject()
  {
    var wrapper = new StreamWrapper(
      "https://testing.speckle.dev/streams/a75ab4f10f/objects/5530363e6d51c904903dafc3ea1d2ec6"
    );
    Assert.That(wrapper.Type, Is.EqualTo(StreamWrapperType.Object));
  }

  [Test]
  public void ParseCommit()
  {
    var wrapper = new StreamWrapper("https://testing.speckle.dev/streams/4c3ce1459c/commits/8b9b831792");
    Assert.That(wrapper.Type, Is.EqualTo(StreamWrapperType.Commit));
  }

  [Test]
  public void ParseGlobalAsBranch()
  {
    var wrapper = new StreamWrapper("https://testing.speckle.dev/streams/0c6ad366c4/globals/");
    Assert.That(wrapper.Type, Is.EqualTo(StreamWrapperType.Branch));
  }

  [Test]
  public void ParseGlobalAsCommit()
  {
    var wrapper = new StreamWrapper("https://testing.speckle.dev/streams/0c6ad366c4/globals/abd3787893");
    Assert.That(wrapper.Type, Is.EqualTo(StreamWrapperType.Commit));
  }
}
