using Speckle.Core.Api;
using Speckle.Core.Api.GraphQL.Inputs;
using Speckle.Core.Api.GraphQL.Models;
using Speckle.Core.Api.GraphQL.Resources;
using Speckle.Core.Credentials;

namespace Speckle.Core.Tests.Integration.API.GraphQL.Resources;

[TestOf(typeof(ProjectResource))]
public class ProjectInviteResourceTests : ResourcesTests
{
  private ProjectInviteResource Sut => FirstUser.ProjectInvite;
  private Project _project;

  private PendingStreamCollaborator _createdInvite;

  protected override async Task OneTimeSetup()
  {
    await base.OneTimeSetup();
    _project = await FirstUser.Project.Create(new("test", null, null));
  }

  [Test, Order(1)]
  public async Task ProjectInviteCreate_By_Email()
  {
    ProjectInviteCreateInput input = new(SecondUser.Account.userInfo.email, null, null, null);
    var res = await Sut.Create(_project.id, input);

    Assert.That(res, Has.Property(nameof(Project.id)).EqualTo(_project.id));
    Assert.That(res.invitedTeam, Has.Count.EqualTo(1));
    Assert.That(res.invitedTeam[0].user.id, Is.EqualTo(SecondUser.Account.userInfo.id));

    _createdInvite = res.invitedTeam[0];
  }

  [Test, Order(3)]
  public async Task ProjectInviteCreate_By_UserId()
  {
    UserInfo invitee = SecondUser.Account.userInfo;
    ProjectInviteCreateInput input = new(null, null, null, invitee.id);
    var res = await Sut.Create(_project.id, input);

    Assert.That(res, Has.Property(nameof(Project.id)).EqualTo(_project.id));
    Assert.That(res.invitedTeam, Has.Count.EqualTo(1));
    Assert.That(res.invitedTeam[0].user.id, Is.EqualTo(invitee.id));
  }

  [Test]
  public void ProjectInviteCreate_InvalidInput()
  {
    Assert.ThrowsAsync<ArgumentException>(async () =>
    {
      var input = new ProjectInviteCreateInput(null, null, null, null);
      await Sut.Create(_project.id, input);
    });

    Assert.ThrowsAsync<ArgumentException>(async () =>
    {
      var input = new ProjectInviteCreateInput(null, "something", "something", null);
      await Sut.Create(_project.id, input);
    });
  }

  [Test, Order(2)]
  public async Task ProjectInviteGet()
  {
    PendingStreamCollaborator collaborator = await Sut.Get(_project.id, _createdInvite.token);

    Assert.That(collaborator, Has.Property(nameof(PendingStreamCollaborator.id)).EqualTo(_createdInvite.id));
    Assert.That(
      collaborator,
      Has.Property(nameof(PendingStreamCollaborator.inviteId)).EqualTo(_createdInvite.inviteId)
    );
  }

  [Test, Order(4)]
  public async Task ProjectInviteUse()
  {
    ProjectInviteUseInput input = new(true, _createdInvite.streamId, _createdInvite.token);
    var res = await Sut.Use(input);

    Assert.That(res, Is.True);
  }
}
