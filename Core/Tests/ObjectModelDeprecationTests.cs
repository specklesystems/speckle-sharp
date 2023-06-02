using NUnit.Framework;
using Speckle.Core.Models;
using Speckle.Core.Serialisation.Deprecated;

namespace Speckle.Core.Serialisation
{
  [TestFixture, TestOf(typeof(BaseObjectSerializer))]
  public class ObjectModelDeprecationTests
  {
    [Test]
    public void GetDeprecatedAtomicType()
    {
      string destinationType = $"Speckle.Core.Serialisation.{nameof(MySpeckleBase)}";

      var result = SerializationUtilities.GetAtomicType(destinationType);
      Assert.That(result, Is.EqualTo(typeof(MySpeckleBase)));
    }

    [Test]
    [TestCase("Objects.Geometry.Mesh", "Objects.Geometry.Deprecated.Mesh")]
    [TestCase("Objects.Mesh", "Objects.Deprecated.Mesh")]
    public void GetDeprecatedTypeName(string input, string expected)
    {
      var actual = SerializationUtilities.GetDeprecatedTypeName(input);
      Assert.That(actual, Is.EqualTo(expected));
    }
  }
}

namespace Speckle.Core.Serialisation.Deprecated
{
  public class MySpeckleBase : Base { }
}
