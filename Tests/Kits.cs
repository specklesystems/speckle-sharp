using System;
using Speckle.Core.Kits;
using NUnit.Framework;
using System.Linq;

namespace Tests
{
  [TestFixture]
  public class Kits
  {
    [Test]
    public void KitsExist()
    {
      var kits = KitManager.Kits;
      Assert.Greater(kits.Count(), 0);

      var types = KitManager.Types;
      Assert.Greater(types.Count(), 0);
    }

    [Test]
    [Ignore("Not going to work unless you have a kit installed.")]
    public void LoadConverter()
    {
      var kits = KitManager.Kits;
      var cp = kits;
      var objsk = kits.ElementAt(2);

      var conv = objsk.Converters;
      var cpc = conv;
    }


  }
}
