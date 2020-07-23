using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Speckle.Core;
using Speckle.Core.GqlModels;
using Speckle.Credentials;

namespace IntegrationTests
{
  public class RemoteOps
  {
    public Remote myRemote;

    [OneTimeSetUp]
    public void Setup()
    {
      myRemote = new Remote(AccountManager.GetAccounts().First());
    }

    [Test]
    public async Task UserGet()
    {
      var res = await myRemote.UserGet();

      Assert.NotNull(res);
    }

    [Test, Order(0)]
    public async Task StreamCreate()
    {
      var res = await myRemote.StreamCreate(new StreamCreateInput
      {
        description = "Hello World",
        name = "Super Stream 01"
      });

      Assert.NotNull(res);
      myRemote.StreamId = res;
    }

    [Test, Order(10)]
    public async Task StreamsGet()
    {
      var res = await myRemote.StreamsGet();

      Assert.NotNull(res);
    }

    [Test, Order(11)]
    public async Task StreamGet()
    {
      var res = await myRemote.StreamGet(myRemote.StreamId);

      Assert.NotNull(res);
      Assert.Equals("master", res.branches.items[0].name);
      Assert.IsNotEmpty(res.collaborators);
    }

    [Test, Order(20)]
    public async Task StreamUpdate()
    {
      var res = await myRemote.StreamUpdate(new StreamUpdateInput
      {
        id = myRemote.StreamId,
        description = "Hello World",
        name = "Super Stream 01 EDITED"
      });

      Assert.IsTrue(res);
    }

    [Test, Order(30)]
    public async Task StreamGrantPermission()
    {
      var res = await myRemote.StreamGrantPermission(
      
        myRemote.StreamId,
        "b4b7f800ac", //TODO: get user id dynamically
        "stream:owner"
      );

      Assert.IsTrue(res);
    }

    [Test, Order(40)]
    public async Task StreamRevokePermission()
    {
      var res = await myRemote.StreamRevokePermission(

        myRemote.StreamId,
        "b4b7f800ac" //TODO: get user id dynamically
      );

      Assert.IsTrue(res);
    }

    [Test, Order(50)]
    public async Task StreamDelete()
    {
      var res = await myRemote.StreamDelete(myRemote.StreamId);
      Assert.IsTrue(res);
    }


  }
}