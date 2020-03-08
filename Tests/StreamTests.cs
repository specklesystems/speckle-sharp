using System;
using Xunit;
using Speckle.Serialisation;
using Speckle.Models;
using System.Collections.Generic;
using Speckle.Kits;
using Speckle.Core;
using Speckle.Transports;
using System.Diagnostics;
using Xunit.Abstractions;

namespace Tests
{
  public class Streams
  {
    [Fact]
    public void Test()
    {
      var myModel = new Stream();
      myModel.Name = "Testing Model";

      var myState = new List<Base>()
      {
        new Point(1,3,4), new Point(4,3,2), new DiningTable(), new Polyline() { Points = new List<Point>(){ new Point(1,3,4), new Point(4,3,2)}}
      };

      for (int i = 0; i < 100; i++)
      {
        if (i % 3 == 0)
          myState.Add(new Polyline() { Points = new List<Point>() { new Point(1, i, i), new Point(4, 3, i) } });
        else
          myState.Add(new DiningTable() { TableModel = i + "-table" });
      }

      myModel.SetState(myState);
      myModel.Commit("first commit");
    }

  }
}
