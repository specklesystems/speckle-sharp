using System;
using System.Collections;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;

namespace RhinoAutoTest
{
  [Collection("RhinoTestingCollection")]
  [TestCaseOrderer("RhinoAutoTest.RhinoTestOrderer", "RhinoAutoTest")]
  public class SendingTests : BaseTest
  {
    [Theory]
    [ClassData(typeof(RhinoTestingFileData))]
    public void Can_SendAllElements(string path)
    {
      var commitId = SendAllElements(path).Result;
      Assert.NotNull(commitId);
      Assert.NotEmpty(commitId);
    }
    
    public SendingTests(RhinoTestFixture fixture) : base(fixture)
    {
    }
    
    
  }
  
  public class RhinoTestingFileData : IEnumerable<object[]>
  {
    public IEnumerator<object[]> GetEnumerator()
    {
      yield return new object[] { @"C:\spockle\Rhino_Standard.3dm" };
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
  }
}
