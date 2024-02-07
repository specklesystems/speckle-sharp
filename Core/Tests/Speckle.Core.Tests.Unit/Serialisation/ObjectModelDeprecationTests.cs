using NUnit.Framework;
using Speckle.Core.Models;
using Speckle.Core.Serialisation.Deprecated;
using Speckle.Core.Serialisation.SerializationUtilities;

namespace Speckle.Core.Tests.Unit.Serialisation
{
  [TestFixture]
  [TestOf(typeof(BaseObjectSerializationUtilities))]
  public class ObjectModelDeprecationTests
  {
    [Test]
    public void GetDeprecatedAtomicType()
    {
      string destinationType = $"Speckle.Core.Serialisation.{nameof(MySpeckleBase)}";

      var result = BaseObjectSerializationUtilities.GetAtomicType(destinationType);
      Assert.That(result, Is.EqualTo(typeof(MySpeckleBase)));
    }

    [Test]
    [TestCase("Objects.Geometry.Mesh", "Objects.Geometry.Deprecated.Mesh")]
    [TestCase("Objects.Mesh", "Objects.Deprecated.Mesh")]
    public void GetDeprecatedTypeName(string input, string expected)
    {
      var actual = BaseObjectSerializationUtilities.GetDeprecatedTypeName(input);
      Assert.That(actual, Is.EqualTo(expected));
    }
  }
}

namespace Speckle.Core.Serialisation.Deprecated
{
  public class MySpeckleBase : Base { }
}
