using Speckle.Core.Api;
using Speckle.Core.Api.GraphQL.Models;
using Speckle.Core.Api.GraphQL.Resources;

namespace Speckle.Core.Tests.Integration.API.GraphQL.Resources;

[TestOf(typeof(ActiveUserResource))]
public class ActiveUserResourceTests : ResourcesTests
{
  private ActiveUserResource Sut => FirstUser.ActiveUser;

  [Test]
  public async Task ActiveUserGet()
  {
    var res = await Sut.Get();
    Assert.That(res.id, Is.EqualTo(FirstUser.Account.userInfo.id));
  }

  [Test]
  public void ActiveUserGet_NonAuthed()
  {
    //TODO: Exceptional cases
    using Client unauthed = new(new() { serverInfo = new() { url = FirstUser.ServerUrl } });

    Assert.ThrowsAsync<SpeckleGraphQLException<ActiveUserResponse>>(async () => _ = await unauthed.ActiveUser.Get()); //TODO: check behaviour
  }

  [Test]
  public async Task ActiveUserGetProjects()
  {
    var p1 = await FirstUser.Project.Create(new("Project 1", null, null));
    var p2 = await FirstUser.Project.Create(new("Project 2", null, null));

    var res = await Sut.GetProjects();

    Assert.That(res.items, Has.Exactly(1).Items.With.Property(nameof(Project.id)).EqualTo(p1.id));
    Assert.That(res.items, Has.Exactly(1).Items.With.Property(nameof(Project.id)).EqualTo(p2.id));
    Assert.That(res.items, Has.Count.EqualTo(2));
  }
}
