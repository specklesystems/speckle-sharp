using NUnit.Framework;
using Speckle.Core.Models;

namespace TestsUnit;

[TestFixture]
public class SpeckleTypeTests
{
  [Test, TestCaseSource(nameof(Cases))]
  public void SpeckleTypeIsProperlyBuilt(Base foo, string expected_type)
  {
    Assert.That(expected_type, Is.EqualTo(foo.speckle_type));
  }

  private static object[] Cases =
  {
    new object[] { new Base(), "Base" },
    new object[] { new Foo(), "TestsUnit.Foo" },
    new object[] { new Bar(), "TestsUnit.Foo:TestsUnit.Bar" },
    new object[] { new Baz(), "TestsUnit.Foo:TestsUnit.Bar:TestsUnit.Baz" }
  };
}

public class Foo : Base { }

public class Bar : Foo { }

public class Baz : Bar { }
