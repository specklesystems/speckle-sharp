using NUnit.Framework;
using Speckle.Core.Models;
using Speckle.Core.Models.Extensions;

namespace Speckle.Core.Tests.Unit.Models.Extensions;

[TestFixture]
[TestOf(nameof(BaseExtensions))]
public class BaseExtensionsTests
{
  [Test]
  [TestCase("myDynamicProp")]
  [TestCase("elements")]
  public void GetDetachedPropName_Dynamic(string propertyName)
  {
    var data = new TestBase();

    var result = data.GetDetachedPropName(propertyName);
    var expected = $"@{propertyName}";
    Assert.That(result, Is.EqualTo(expected));
  }

  [Test]
  [TestCase(nameof(TestBase.myProperty))]
  [TestCase(nameof(TestBase.myOtherProperty))]
  public void GetDetachedPropName_Instance(string propertyName)
  {
    var data = new TestBase();
    var result = data.GetDetachedPropName(propertyName);

    Assert.That(result, Is.EqualTo(propertyName));
  }

  public class TestBase : Base
  {
    public string myProperty { get; set; }
    public string myOtherProperty { get; set; }
  }
}
