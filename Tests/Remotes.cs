using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Speckle.Core;
using Speckle.Models;

namespace Tests
{
  [TestFixture]
  public class Remotes
  {

    Stream MyTestStream;

    [SetUp]
    public void SetUp()
    {
      // Id is hard coded so we don't garbage things up.
      MyTestStream = new Stream() { Name = "My Test Stream", Id = "5f5e353b-59b1-47f2-a747-f21a1b4b6f09" };

      var myState = new List<Base>()
      {
        new Point(1,3,4),
        new Point(4,3,2),
        new DiningTable(),
        new Polyline() { Points = new List<Point>(){ new Point(1,3,4), new Point(4,3,2)}}
      };

      MyTestStream.Add(myState);
      MyTestStream.Commit("first commit");

      myState = new List<Base>()
      {
        new Point(1,3,4),
        new Point(4,3,2)
      };
      MyTestStream.SetState(myState);
      MyTestStream.Commit("second commit");

      myState = new List<Base>();
      var numObjs = 1;
      for (int i = 0; i < numObjs; i++)
      {
        myState.Add(new Point(0, i, i * 9));
      }

      var nestedTable = new DiningTable();
      ((dynamic)nestedTable)["@superProperty"] = new Point(42, 4242, 42);

      myState.Add(new DiningTable()); // Add an object with nested sub-objects.

      MyTestStream.SetState(myState);
      MyTestStream.Commit("added one thousand points.");
    }


    [Test, Order(1)]
    public void Push()
    {
      MyTestStream.AddRemote(new Remote(Account.GetLocalAccounts().First(), "Mock Remote"));

      MyTestStream.Publish("Mock Remote", "master");

      var test = MyTestStream;
    }

    [Test, Order(2)]
    public void Pull()
    {
      Assert.Fail();
    }
  }
}
