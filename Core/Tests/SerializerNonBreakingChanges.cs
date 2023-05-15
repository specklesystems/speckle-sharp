using System.Drawing;
using NUnit.Framework;
using Speckle.Core.Api;
using Speckle.Core.Models;

namespace Tests.Models;

/// <summary>
/// Test fixture that documents what property typing changes maintain backwards/cross/forwards compatibility, and are "non-breaking" changes.
/// This doesn't guarantee things work this way for SpecklePy
/// Nor does it encompass other tricks (like deserialize callback, or computed json ignored properties)
/// </summary>
[
  TestFixture,
  Description("For certain types, changing property from one type to another should be implicitly backwards compatible")
]
public class SerializerNonBreakingChanges : PrimitiveTestFixture
{
  [Test, TestCaseSource(nameof(Int8TestCases)), TestCaseSource(nameof(Int32TestCases))]
  public void IntToColor(int argb)
  {
    var from = new IntValueMock { value = argb };

    var res = from.SerializeAsTAndDeserialize<ColorValueMock>();
    Assert.That(res.value.ToArgb(), Is.EqualTo(argb));
  }

  [Test, TestCaseSource(nameof(Int8TestCases)), TestCaseSource(nameof(Int32TestCases))]
  public void ColorToInt(int argb)
  {
    var from = new ColorValueMock { value = Color.FromArgb(argb) };

    var res = from.SerializeAsTAndDeserialize<IntValueMock>();
    Assert.That(res.value, Is.EqualTo(argb));
  }

  [
    Test,
    TestCaseSource(nameof(Int8TestCases)),
    TestCaseSource(nameof(Int32TestCases)),
    TestCaseSource(nameof(Int64TestCases))
  ]
  public void IntToDouble(long testCase)
  {
    var from = new IntValueMock { value = testCase };

    var res = from.SerializeAsTAndDeserialize<DoubleValueMock>();
    Assert.That(res.value, Is.EqualTo(testCase));
  }

  [
    Test,
    TestCaseSource(nameof(Int8TestCases)),
    TestCaseSource(nameof(Int32TestCases)),
    TestCaseSource(nameof(Int64TestCases))
  ]
  public void IntToString(long testCase)
  {
    var from = new IntValueMock { value = testCase };

    var res = from.SerializeAsTAndDeserialize<StringValueMock>();
    Assert.That(res.value, Is.EqualTo(testCase.ToString()));
  }

  private static double[][] ArrayTestCases =
  {
    new double[] { },
    new double[] { 0, 1, int.MaxValue, int.MinValue },
    new[] { default, double.Epsilon, double.MaxValue, double.MinValue }
  };

  [Test, TestCaseSource(nameof(ArrayTestCases))]
  public void ArrayToList(double[] testCase)
  {
    var from = new ArrayDoubleValueMock { value = testCase };

    var res = from.SerializeAsTAndDeserialize<ListDoubleValueMock>();
    Assert.That(res.value, Is.EquivalentTo(testCase));
  }

  [Test, TestCaseSource(nameof(ArrayTestCases))]
  public void ListToArray(double[] testCase)
  {
    var from = new ListDoubleValueMock { value = testCase.ToList() };

    var res = from.SerializeAsTAndDeserialize<ArrayDoubleValueMock>();
    Assert.That(res.value, Is.EquivalentTo(testCase));
  }

  [Test, TestCaseSource(nameof(MyEnums))]
  public void EnumToInt(MyEnum testCase)
  {
    var from = new EnumValueMock { value = testCase };

    var res = from.SerializeAsTAndDeserialize<IntValueMock>();
    Assert.That(res.value, Is.EqualTo((int)testCase));
  }

  [Test, TestCaseSource(nameof(MyEnums))]
  public void IntToEnum(MyEnum testCase)
  {
    var from = new IntValueMock { value = (int)testCase };

    var res = from.SerializeAsTAndDeserialize<EnumValueMock>();
    Assert.That(res.value, Is.EqualTo(testCase));
  }

  [Test]
  [TestCaseSource(nameof(Float64TestCases))]
  [TestCaseSource(nameof(Float32TestCases))]
  public void DoubleToDouble(double testCase)
  {
    var from = new DoubleValueMock { value = testCase };

    var res = from.SerializeAsTAndDeserialize<DoubleValueMock>();
    Assert.That(res.value, Is.EqualTo(testCase));
  }
}

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

public class TValueMock<T> : SerializerMock
{
  public T value { get; set; }
}

public class ListDoubleValueMock : SerializerMock
{
  public List<double> value { get; set; }
}

public class ArrayDoubleValueMock : SerializerMock
{
  public double[] value { get; set; }
}

public class IntValueMock : SerializerMock
{
  public long value { get; set; }
}

public class StringValueMock : SerializerMock
{
  public string value { get; set; }
}

public class DoubleValueMock : SerializerMock
{
  public double value { get; set; }
}

public class ColorValueMock : SerializerMock
{
  public Color value { get; set; }
}

public class EnumValueMock : SerializerMock
{
  public MyEnum value { get; set; }
}

public enum MyEnum
{
  Zero,
  One,
  Two,
  Three,
  Neg = -1,
  Min = int.MinValue,
  Max = int.MaxValue
}

public abstract class SerializerMock : Base
{
  public string _speckle_type;

  protected SerializerMock()
  {
    _speckle_type = base.speckle_type;
  }

  public override string speckle_type => _speckle_type;

  public void SerializeAs<T>() where T : Base, new()
  {
    T target = new();
    _speckle_type = target.speckle_type;
  }

  internal TTo SerializeAsTAndDeserialize<TTo>() where TTo : Base, new()
  {
    SerializeAs<TTo>();

    var json = Operations.Serialize(this);

    Base result = Operations.Deserialize(json);
    Assert.NotNull(result);
    Assert.That(result, Is.TypeOf<TTo>());
    return (TTo)result;
  }
}

public abstract class PrimitiveTestFixture
{
  public static sbyte[] Int8TestCases = { default, sbyte.MaxValue, sbyte.MinValue };
  public static short[] Int16TestCases = { short.MaxValue, short.MinValue };
  public static int[] Int32TestCases = { int.MinValue, int.MaxValue };
  public static long[] Int64TestCases = { long.MaxValue, long.MinValue };

  public static double[] Float64TestCases =
  {
    default,
    double.Epsilon,
    double.MaxValue,
    double.MinValue,
    double.PositiveInfinity,
    double.NegativeInfinity,
    double.NaN
  };

  public static float[] Float32TestCases =
  {
    default,
    float.Epsilon,
    float.MaxValue,
    float.MinValue,
    float.PositiveInfinity,
    float.NegativeInfinity,
    float.NaN
  };

  public static Half[] Float16TestCases =
  {
    default,
    Half.Epsilon,
    Half.MaxValue,
    Half.MinValue,
    Half.PositiveInfinity,
    Half.NegativeInfinity,
    Half.NaN
  };
  public static float[] FloatIntegralTestCases = { 0, 1, int.MaxValue, int.MinValue };

  public static MyEnum[] MyEnums => Enum.GetValues(typeof(MyEnum)).Cast<MyEnum>().ToArray();
}
