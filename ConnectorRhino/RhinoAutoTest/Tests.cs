using System;
using IndustrialConstructionUnitTests;
using Xunit;

namespace RhinoAutoTest
{
  [Collection("RhinoTestingCollection")]
  public class Tests : BaseTest
  {
    [Theory]
    [InlineData("G:\\Shared drives\\All Company\\07 Sample Models\\TESTING\\01 Standard\\Rhino_Standard.3dm")]
    public void Test1(string path)
    {
      var commitId = SendAllElements(path).Result;
      Assert.NotNull(commitId);
      Assert.NotEmpty(commitId);
    }

    public Tests(RhinoTestFixture fixture) : base(fixture)
    {
    }
  }
}
