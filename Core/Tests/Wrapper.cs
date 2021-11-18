using NUnit.Framework;
using Speckle.Core.Credentials;
using TestsUnit;

namespace Tests
{
  [TestFixture]
  public class WrapperTests
  {
    [Test]
    public void ParseStream()
    {
      var wrapper = new StreamWrapper("https://testing.speckle.dev/streams/a75ab4f10f");
      Assert.AreEqual(StreamWrapperType.Stream, wrapper.Type);
    }

    [Test]
    public void ParseBranch()
    {
      var wrapperCrazy = new StreamWrapper("https://testing.speckle.dev/streams/4c3ce1459c/branches/%F0%9F%8D%95%E2%AC%85%F0%9F%8C%9F%20you%20wat%3F");
      Assert.AreEqual(wrapperCrazy.BranchName, "🍕⬅🌟 you wat?");
      Assert.AreEqual(StreamWrapperType.Branch, wrapperCrazy.Type);

      wrapperCrazy = new StreamWrapper("https://testing.speckle.dev/streams/4c3ce1459c/branches/next%20level");
      Assert.AreEqual(wrapperCrazy.BranchName, "next level");
      Assert.AreEqual(StreamWrapperType.Branch, wrapperCrazy.Type);
    }

    [Test]
    public void ParseObject()
    {
      var wrapper = new StreamWrapper("https://testing.speckle.dev/streams/a75ab4f10f/objects/5530363e6d51c904903dafc3ea1d2ec6");
      Assert.AreEqual(StreamWrapperType.Object, wrapper.Type);
    }

    [Test]
    public void ParseCommit()
    {
      var wrapper = new StreamWrapper("https://testing.speckle.dev/streams/4c3ce1459c/commits/8b9b831792");
      Assert.AreEqual(StreamWrapperType.Commit, wrapper.Type);
    }

    [Test]
    public void ParseGlobalAsBranch()
    {
      var wrapper = new StreamWrapper("https://testing.speckle.dev/streams/0c6ad366c4/globals/");
      Assert.AreEqual(StreamWrapperType.Branch, wrapper.Type);
    }

    [Test]
    public void ParseGlobalAsCommit()
    {
      var wrapper = new StreamWrapper("https://testing.speckle.dev/streams/0c6ad366c4/globals/abd3787893");
      Assert.AreEqual(StreamWrapperType.Commit, wrapper.Type);
    }
  }
}
