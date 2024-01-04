using NUnit.Framework;
using Speckle.Core.Models;

namespace Speckle.Core.Tests.Unit.Models;

[TestFixture]
[TestOf(typeof(Base))]
public class SpeckleTypeTests
{
  [Test, TestCaseSource(nameof(s_cases))]
  public void SpeckleTypeIsProperlyBuilt(Base foo, string expectedType)
  {
    Assert.That(foo.speckle_type, Is.EqualTo(expectedType));
  }

  private static readonly object[] s_cases =
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
