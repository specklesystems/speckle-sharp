using NUnit.Framework;
using Speckle.Core.Kits;

namespace Speckle.Core.Tests.Unit.Kits;

[TestFixture]
[TestOf(typeof(KitManager))]
public class KitManagerTests
{
  [Test]
  public void KitsExist()
  {
    var kits = KitManager.Kits.ToArray();
    Assert.That(kits, Has.Length.GreaterThan(0));

    var types = KitManager.Types.ToArray();
    Assert.That(types, Has.Length.GreaterThan(0));
  }
}
