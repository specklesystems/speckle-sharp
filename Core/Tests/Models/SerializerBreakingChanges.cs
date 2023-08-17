using NUnit.Framework;

namespace TestsUnit.Models;

/// <summary>
/// Test fixture that documents what property typing changes break backwards/cross/forwards compatibility, and are "breaking" changes.
/// This doesn't guarantee things work this way for SpecklePy
/// Nor does it encompass other tricks (like deserialize callback, or computed json ignored properties)
/// </summary>
[
  TestFixture,
  Description(
    "For certain types, changing property from one type to another is a breaking change, and not backwards/forwards compatible"
  )
]
public class SerializerBreakingChanges : PrimitiveTestFixture
{
  [Test]
  public void StringToInt_ShouldThrow()
  {
    var from = new StringValueMock();
    from.value = "testValue";
    Assert.Throws<Exception>(() => from.SerializeAsTAndDeserialize<IntValueMock>());
  }

  [Test, TestCaseSource(nameof(MyEnums))]
  public void StringToEnum_ShouldThrow(MyEnum testCase)
  {
    var from = new StringValueMock { value = testCase.ToString() };

    Assert.Throws<Exception>(() =>
    {
      var res = from.SerializeAsTAndDeserialize<EnumValueMock>();
    });
  }

  [
    Test,
    Description("Deserialization of a JTokenType.Float to a .NET short/int/long should throw exception"),
    TestCaseSource(nameof(Float64TestCases)),
    TestCase(1e+30)
  ]
  public void DoubleToInt_ShouldThrow(double testCase)
  {
    var from = new DoubleValueMock { value = testCase };
    Assert.Throws<Exception>(() => from.SerializeAsTAndDeserialize<IntValueMock>());
  }
}
