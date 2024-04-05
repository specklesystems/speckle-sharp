using NUnit.Framework;
using Speckle.Core.Helpers;

namespace Speckle.Core.Tests.Unit.Models;

[TestFixture(TestOf = typeof(Crypt))]
public sealed class UtilitiesTests
{
  [Test]
  [TestOf(nameof(Crypt.Md5))]
  [TestCase("WnAbz1hCznVmDh1", "ad48ff1e60ea2369de178aaab2fa99af")]
  [TestCase("wQKrSUzBB7FI1o6", "2424cff4a88055b149e5ff2aaf0b3131")]
  public void Md5(string input, string expected)
  {
    var lower = Crypt.Md5(input, "x2");
    var upper = Crypt.Md5(input, "X2");
    Assert.That(lower, Is.EqualTo(expected.ToLower()));
    Assert.That(upper, Is.EqualTo(expected.ToUpper()));
  }

  [TestCase("fxFB14cBcXvoENN", "a6b48b2514a3ded45ad2cbea9e325c25c7ddc998247f2aff9bdd0e2694f5d5d4")]
  [TestCase("tgWsOH8frdAwJT7", "82ac4675b283bae908fd110095252eca87dc6080244fc2014cf61bd9e45d37fc")]
  [TestOf(nameof(Crypt.Sha256))]
  public void Sha256(string input, string expected)
  {
    var lower = Crypt.Sha256(input, "x2");
    var upper = Crypt.Sha256(input, "X2");
    Assert.That(lower, Is.EqualTo(expected.ToLower()));
    Assert.That(upper, Is.EqualTo(expected.ToUpper()));
  }

  [Test]
  public void FlattenToNativeConversion()
  {
    var singleObject = new object();
    var nestedObjects = new List<object>()
    {
      new List<object>()
      {
        new(), // obj 1
        new() // obj 2
      },
      new() // obj 3
    };

    var singleObjectFlattened = Core.Models.Utilities.FlattenToNativeConversionResult(singleObject);
    var nestedObjectsFlattened = Core.Models.Utilities.FlattenToNativeConversionResult(nestedObjects);

    Assert.That(singleObjectFlattened.Count, Is.EqualTo(1));
    Assert.That(nestedObjectsFlattened.Count, Is.EqualTo(3));
  }
}
