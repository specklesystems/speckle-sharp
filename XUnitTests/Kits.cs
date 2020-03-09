using System;
using Speckle.Kits;
using Xunit;

namespace Tests
{
  public class Kits
  {
    [Fact]
    public void KitsExist()
    {
      var kits = KitManager.Kits;
      Assert.NotEmpty(kits);

      var types = KitManager.Types;
      Assert.NotEmpty(types);
    }
  }
}
