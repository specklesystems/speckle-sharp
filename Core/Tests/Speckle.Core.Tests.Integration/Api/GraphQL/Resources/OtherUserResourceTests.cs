using Speckle.Core.Api;
using Speckle.Core.Api.GraphQL.Resources;
using Speckle.Core.Credentials;

namespace Speckle.Core.Tests.Integration.API.GraphQL.Resources;

[TestOf(typeof(OtherUserResource))]
public class OtherUserResourceTests
{
  private Client _testUser;
  private Account _testData;
  private OtherUserResource Sut => _testUser.OtherUser;

  [OneTimeSetUp]
  public async Task Setup()
  {
    _testUser = await Fixtures.SeedUserWithClient();
    _testData = await Fixtures.SeedUser();
  }

  [Test]
  public async Task OtherUserGet()
  {
    var res = await Sut.Get(_testData.userInfo.id);
    Assert.That(res.name, Is.EqualTo(_testData.userInfo.name));
  }

  [Test]
  public async Task OtherUserGet_NonExistentUser()
  {
    var result = await Sut.Get("AnIdThatDoesntExist");
    Assert.That(result, Is.Null);
  }

  [Test]
  public async Task UserSearch()
  {
    var res = await Sut.UserSearch(_testData.userInfo.email, 25);
    Assert.That(res.items, Has.Count.EqualTo(1));
    Assert.That(res.items[0].id, Is.EqualTo(_testData.userInfo.id));
  }

  [Test]
  public async Task UserSearch_NonExistentUser()
  {
    var res = await Sut.UserSearch("idontexist@example.com", 25);
    Assert.That(res.items, Has.Count.EqualTo(0));
  }
}
