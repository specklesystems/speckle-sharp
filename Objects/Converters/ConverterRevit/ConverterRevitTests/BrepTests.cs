using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Xunit;

namespace ConverterRevitTests
{
  public class BrepFixture: SpeckleConversionFixture
  {
    public override string TestFile => throw new NotImplementedException();
    public override string NewFile => throw new NotImplementedException();

    public override List<BuiltInCategory> Categories => new List<BuiltInCategory> { BuiltInCategory.OST_Mass, BuiltInCategory.OST_Mass };  }
  
  public class BrepTests : SpeckleConversionTest, IClassFixture<BrepFixture>
  {
    public BrepTests(BrepFixture fixture)
    {
      this.fixture = fixture;
    }

    [Fact]
    public void BrepToNative()
    {
      throw new NotImplementedException();
    }

    [Fact]
    public void BrepToSpeckle()
    {
      throw new NotImplementedException();
    }
  }

}