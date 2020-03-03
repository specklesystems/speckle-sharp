using System;
using Xunit;
using Speckle.Serialisation;
using Speckle.Models;
using System.Collections.Generic;
using Speckle.Kits;
using System.Linq;

namespace Tests
{
  public class Serialization
  {
    [Fact]
    public void KitsExist()
    {
      var kits = KitManager.Kits;
      Assert.NotEmpty(kits);

      var types = KitManager.Types;
      Assert.NotEmpty(types);
    }

    [Fact]
    public void Test1()
    {
      var serializer = new JsonConverter();

      var table = new DiningTable();
      var fixture = new TableLegFixture();

      ((dynamic)table).RANDOM = new TableLegFixture();

      var result = serializer.Serialize(table);
      var copy = result;

      var copyThree = serializer.SerializeAndSave(table, null);
      var copyFour = copyThree;

      var test = serializer.Deserialize(result);
    }
  }
}
