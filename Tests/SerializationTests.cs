using System;
using Xunit;
using Speckle.Serialisation;
using Speckle.Models;
using System.Collections.Generic;
using Speckle.Kits;
using System.Linq;
using Speckle.Transports;

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
    public void SimpleSerialization()
    {
      var serializer = new JsonConverter();

      var table = new DiningTable();
      ((dynamic)table)["@wonkyVariable_Name"] = new TableLegFixture();

      var result = serializer.Serialize(table);

      var test = serializer.Deserialize(result);

      Assert.Equal(test.hash, table.hash);
    }

    [Fact]
    public void TransportSerialization()
    {
      var transport = new MemoryTransport();
      var serializer = new JsonConverter();

      var table = new DiningTable();

      var result = serializer.SerializeAndSave(table, transport);

      var test = serializer.DeserializeAndGet(result, transport);

      Assert.Equal(test.hash, table.hash);
    }
  }
}
