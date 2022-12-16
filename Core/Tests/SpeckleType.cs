using NUnit.Framework;
using Speckle.Core.Models;

namespace Tests
{
  [TestFixture]
  public class SpeckleTypeTests
  {
    [Test]
    [TestCaseSource(nameof(Cases))]
    public void SpeckleTypeIsProperlyBuilt(Base foo, string expected_type)
    {
      Assert.AreEqual(foo.speckle_type, expected_type);
    }

    static object[] Cases = {
      new object[] {new Base(), "Base"},
      new object[] {new Foo(), "Tests.Foo"},
      new object[] {new Bar(), "Tests.Foo:Tests.Bar"},
      new object[] {new Baz(), "Tests.Foo:Tests.Bar:Tests.Baz"},
    };

  }
  public class Foo: Base {}
  public class Bar: Foo {}
  public class Baz: Bar {}
}