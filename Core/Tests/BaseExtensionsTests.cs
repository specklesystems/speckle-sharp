using NUnit.Framework;
using Speckle.Core.Models;
using Speckle.Core.Models.Extensions;

namespace TestsUnit;

[TestFixture, TestOf(nameof(BaseExtensions))]
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


  [Test]
  [TestCase("walls")]
  [TestCase("@walls")]
  public void GetCollectionElements(string propertyName)
  {
    var data1 = new TestCollection();
    var data2 = new TestBase2();

    var result1 = data1.GetCollectionElements(propertyName);
    var result2 = data2.GetCollectionElements(propertyName);

    Assert.That(result1[0].applicationId, Is.EqualTo(result2[0].applicationId));
  }

  public class TestBase : Base
  {
    public string myProperty { get; set; }
    public string myOtherProperty { get; set; }
  }

  public class TestCollection : Collection
  {
    public TestCollection()
    {
      elements = new List<Base>();
      elements.Add(new Collection("walls", "?") { elements = new List<Base> { new Base { applicationId = "wall1" } } });
      this["@walls"] = new List<Base> { new Base { applicationId = "wall1" } };
      this["walls"] = new List<Base> { new Base { applicationId = "wall1" } };

    }
  }

  public class TestBase2 : Base
  {
    public TestBase2()
    {
      this["@walls"] = new List<Base> { new Base { applicationId = "wall1" } };
      //this["walls"] = new List<Base> { new Base { applicationId = "wall1" } };

    }
  }

}
