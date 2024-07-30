using NUnit.Framework;
using Speckle.Core.Models;
using Speckle.Core.Models.Extensions;

namespace Speckle.Core.Tests.Unit.Models.Extensions;

[TestOf(typeof(BaseExtensions))]
public class DisplayValueTests
{
  private const string PAYLOAD = "This is my payload";
  private static readonly Base s_displayValue = new() { applicationId = PAYLOAD };

  [TestCaseSource(nameof(TestCases))]
  public void TestTryGetDisplayValue_WithValue(Base testCase)
  {
    var res = testCase.TryGetDisplayValue();

    Assert.That(res, Has.Count.EqualTo(1));
    Assert.That(res, Has.One.Items.TypeOf<Base>().With.Property(nameof(Base.applicationId)).EqualTo(PAYLOAD));
  }

  public static IEnumerable<Base> TestCases()
  {
    var listOfBase = new List<object> { s_displayValue }; //This is what our deserializer will output
    var listOfMesh = new List<Base> { s_displayValue };
    yield return new Base { ["@displayValue"] = s_displayValue };
    yield return new Base { ["@displayValue"] = s_displayValue };
    yield return new Base { ["displayValue"] = listOfBase };
    yield return new Base { ["displayValue"] = listOfBase };
    yield return new TypedDisplayValue { displayValue = s_displayValue };
    yield return new TypedDisplayValueList { displayValue = listOfMesh };
  }

  private class TypedDisplayValue : Base
  {
    public Base displayValue { get; set; }
  }

  private class TypedDisplayValueList : Base
  {
    public List<Base> displayValue { get; set; }
  }
}
