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

  [TestCase("fxFB14cBcXvoENN", "887db9349afa455f957a95f9dbacbb3c10697749cf4d4afc5c6398932a596fbc")]
  [TestCase("tgWsOH8frdAwJT7", "e486224ded0dcb1452d69d0d005a6dcbc52087f6e8c66e04803e1337a192abb4")]
  [TestOf(nameof(Crypt.Sha256))]
  public void Sha256(string input, string expected)
  {
    var lower = Crypt.Sha256(input, "x2");
    var upper = Crypt.Sha256(input, "X2");
    Assert.That(lower, Is.EqualTo(expected.ToLower()));
    Assert.That(upper, Is.EqualTo(expected.ToUpper()));
  }
}
