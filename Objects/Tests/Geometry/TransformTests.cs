using System.Collections;
using System.DoubleNumerics;
using NUnit.Framework;
using Objects.Other;
using Speckle.Core.Kits;

namespace Objects.Tests.Geometry;

[TestFixture, TestOf(typeof(Transform))]
public class TransformTests
{
  private const float FLOAT_TOLLERANCE = 1e-6f;

  [Test, TestCaseSource(nameof(TransformTestCases))]
  public void ArrayBackAndForth(Matrix4x4 data)
  {
    var start = new Transform(data);
    var asArr = start.ToArray();
    var end = new Transform(asArr);

    Assert.AreEqual(data, end.matrix);
  }

  [Test, TestCaseSource(nameof(TransformTestCases))]
  public void ConvertToUnits(Matrix4x4 data)
  {
    const float SF = 1000f;

    var transpose = Matrix4x4.Transpose(data); //NOTE: Transform expects matrices transposed (translation in column 4)
    var mm = Matrix4x4.Transpose(
      Transform.CreateMatrix(new Transform(transpose, Units.Meters).ConvertToUnits(Units.Millimeters))
    );

    Matrix4x4.Decompose(data, out var ms, out var mr, out var mt);
    Matrix4x4.Decompose(mm, out var mms, out var mmr, out var mmt);

    Assert.Multiple(() =>
    {
      Assert.That(mms.X, Is.EqualTo(ms.X).Within(FLOAT_TOLLERANCE), "Expect scale x to be unchanged");
      Assert.That(mms.Y, Is.EqualTo(ms.Y).Within(FLOAT_TOLLERANCE), "Expect scale y to be unchanged");
      Assert.That(mms.Z, Is.EqualTo(ms.Z).Within(FLOAT_TOLLERANCE), "Expect scale z to be unchanged");

      Assert.That(Quaternion.Dot(mr, mmr), Is.LessThan(1).Within(FLOAT_TOLLERANCE), "Expect rot x to be equivalent");

      Assert.That(mmt.X, Is.EqualTo(mt.X * SF).Within(FLOAT_TOLLERANCE), $"Expect translation x to be scaled by {SF}");
      Assert.That(mmt.Y, Is.EqualTo(mt.Y * SF).Within(FLOAT_TOLLERANCE), $"Expect translation y to be scaled by {SF}");
      Assert.That(mmt.Z, Is.EqualTo(mt.Z * SF).Within(FLOAT_TOLLERANCE), $"Expect translation z to be scaled by {SF}");
    });
  }

  [
    Test(Description = "Tests that Transform decompose matches the behaviour of Matrix4x4"),
    TestCaseSource(nameof(TransformTestCases))
  ]
  public void Decompose(Matrix4x4 data)
  {
    var transpose = Matrix4x4.Transpose(data); //NOTE: Transform expects matrices transposed (translation in column 4)
    var sut = new Transform(transpose);

    var expected = data;
    sut.Decompose(out var s, out var r, out var _t);
    var t = new Vector3(_t.X, _t.Y, _t.Z);
    Matrix4x4.Decompose(expected, out var expectedS, out var expectedR, out var expectedT);

    Assert.Multiple(() =>
    {
      Assert.That(s.X, Is.EqualTo(expectedS.X).Within(FLOAT_TOLLERANCE), "Expect scale x to be unchanged");
      Assert.That(s.Y, Is.EqualTo(expectedS.Y).Within(FLOAT_TOLLERANCE), "Expect scale y to be unchanged");
      Assert.That(s.Z, Is.EqualTo(expectedS.Z).Within(FLOAT_TOLLERANCE), "Expect scale z to be unchanged");

      Assert.That(
        Quaternion.Dot(r, expectedR),
        Is.LessThan(1).Within(FLOAT_TOLLERANCE),
        "Expect rot x to be equivalent"
      );

      Assert.That(t.X, Is.EqualTo(expectedT.X).Within(FLOAT_TOLLERANCE), "Expect translation x to be unchanged");
      Assert.That(t.Y, Is.EqualTo(expectedT.Y).Within(FLOAT_TOLLERANCE), "Expect translation y to be unchanged");
      Assert.That(t.Z, Is.EqualTo(expectedT.Z).Within(FLOAT_TOLLERANCE), "Expect translation z to be unchanged");
    });
  }

  /// <summary>
  /// Set of TRS transforms (row dominant i.e. translation in row 4)
  /// All with non-negative scale and rotation (for ease of testing scale and rot independently)
  /// </summary>
  /// <returns></returns>
  private static IEnumerable TransformTestCases()
  {
    var t = new Vector3(128.128f, 255.255f, 512.512f);
    var r = Quaternion.CreateFromYawPitchRoll(1.9f, 0.6666667f, 0.5f);
    var s = new Vector3(123f, 32f, 0.5f);

    yield return new TestCaseData(Matrix4x4.Identity).SetName("{m} Identity Matrix");

    yield return new TestCaseData(Matrix4x4.CreateTranslation(t)).SetName("{m} Translation Only (positive)");

    yield return new TestCaseData(Matrix4x4.CreateTranslation(t * -Vector3.UnitX)).SetName("{m} Translation Only -X");

    yield return new TestCaseData(Matrix4x4.CreateTranslation(t * -Vector3.UnitY)).SetName("{m} Translation Only -Y");

    yield return new TestCaseData(Matrix4x4.CreateTranslation(t * -Vector3.UnitZ)).SetName("{m} Translation Only -Z");

    yield return new TestCaseData(Matrix4x4.CreateTranslation(-t)).SetName("{m} Translation Only -XYZ ");

    yield return new TestCaseData(Matrix4x4.CreateFromYawPitchRoll(0.5f, 0.0f, 0.0f)).SetName("{m} Rotation Only X ");

    yield return new TestCaseData(Matrix4x4.CreateFromYawPitchRoll(0.0f, 0.5f, 0.0f)).SetName("{m} Rotation Only Y ");

    yield return new TestCaseData(Matrix4x4.CreateFromYawPitchRoll(0.0f, 0.0f, 0.5f)).SetName("{m} Rotation Only Z ");

    yield return new TestCaseData(Matrix4x4.CreateFromYawPitchRoll(0.5f, 0.5f, 0.5f)).SetName("{m} Rotation Only XYZ ");

    yield return new TestCaseData(Matrix4x4.CreateFromQuaternion(r)).SetName("{m} Rotation Only");

    yield return new TestCaseData(Matrix4x4.Identity + Matrix4x4.CreateScale(s)).SetName("{m} Scale Only");

    yield return new TestCaseData(Matrix4x4.CreateTranslation(t) + Matrix4x4.CreateFromQuaternion(r)).SetName(
      "{m} Translation + Rotation"
    );

    yield return new TestCaseData(
      Matrix4x4.CreateTranslation(t) + Matrix4x4.CreateFromQuaternion(r) + Matrix4x4.CreateScale(s)
    ).SetName("{m} Translation + Rotation + Scale");

    yield return new TestCaseData(
      Matrix4x4.CreateTranslation(t) + Matrix4x4.CreateFromQuaternion(r) + Matrix4x4.CreateScale(-s)
    ).SetName("{m} Translation + Rotation + -Scale");
  }
}
