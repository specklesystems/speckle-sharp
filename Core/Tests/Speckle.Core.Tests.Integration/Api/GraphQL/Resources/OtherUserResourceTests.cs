using Speckle.Core.Api;
using Speckle.Core.Api.GraphQL.Models;
using Speckle.Core.Api.GraphQL.Models.Responses;
using Speckle.Core.Api.GraphQL.Resources;

namespace Speckle.Core.Tests.Integration.API.GraphQL.Resources;

[TestOf(typeof(OtherUserResource))]
public class OtherUserResourceTests
{
  private OtherUserResource Sut => ResourcesTestsFixture.FirstUser.OtherUser;
  private UserInfo TestData => ResourcesTestsFixture.SecondUser.Account.userInfo;

  [Test]
  public async Task OtherUserGet()
  {
    var res = await Sut.Get(TestData.id);
    Assert.That(res.name, Is.EqualTo(TestData.name));
  }

  [Test]
  public void OtherUserGet_NonExistentUser()
  {
    Assert.CatchAsync<SpeckleGraphQLException<LimitedUserResponse>>(async () =>
    {
      _ = await Sut.Get("AnIdThatDoesntExist");
    });
  }

  [Test]
  public async Task UserSearch()
  {
    var res = await Sut.UserSearch(TestData.email);
    Assert.That(res, Has.Count.EqualTo(1));
    Assert.That(res[0].id, Is.EqualTo(TestData.id));
  }

  [Test]
  public async Task UserSearch_NonExistentUser()
  {
    var res = await Sut.UserSearch("idontexist@example.com");
    Assert.That(res, Has.Count.EqualTo(0));
  }
}
