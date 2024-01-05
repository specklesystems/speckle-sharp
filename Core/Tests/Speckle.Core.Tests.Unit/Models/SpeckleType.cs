using NUnit.Framework;
using Speckle.Core.Models;
using TestModels;

namespace Speckle.Core.Tests.Unit.Models
{
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
      new object[] { new Foo(), "TestModels.Foo" },
      new object[] { new Bar(), "TestModels.Foo:TestModels.Bar" },
      new object[] { new Baz(), "TestModels.Foo:TestModels.Bar:TestModels.Baz" }
    };
  }
}

namespace TestModels
{
  public class Foo : Base { }

  public class Bar : Foo { }

  public class Baz : Bar { }
}
