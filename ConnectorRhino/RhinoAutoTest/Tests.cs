using System;
using System.Collections;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;

namespace RhinoAutoTest
{
  [Collection("RhinoTestingCollection")]
  public class SendingTests : BaseTest, IDisposable
  {
    
    
    [Theory]
    [ClassData(typeof(RhinoTestingFileData))]
    public void Can_SendAllElements(string path)
    {
      Console.WriteLine("Speckle - Starting!");
      var commitId = SendAllElements(path).Result;
      Console.WriteLine("Speckle - Sent!");

      Assert.NotNull(commitId);
      Assert.NotEmpty(commitId);
      Console.WriteLine("Speckle - Asserted!");

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
