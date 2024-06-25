using Speckle.Core.Api;
using Speckle.Core.Api.GraphQL.Inputs;
using Speckle.Core.Api.GraphQL.Models;
using Speckle.Core.Api.GraphQL.Resources;

namespace Speckle.Core.Tests.Integration.API.GraphQL.Resources;

[TestOf(typeof(ProjectResource))]
public class ProjectInviteResourceTests
{
  private ProjectInviteResource Sut => ResourcesTestsFixture.FirstUser.ProjectInvite;
  private UserInfo TargetUser => ResourcesTestsFixture.SecondUser.Account.userInfo;
  private Project Project => ResourcesTestsFixture.Project;

  private PendingStreamCollaborator _createdInvite;

  [Test]
  public async Task ProjectInviteCreate_By_Email()
  {
    ProjectInviteCreateInput input = new(TargetUser.email, null, null, null);
    var res = await Sut.Create(Project.id, input);

    Assert.That(res, Has.Property(nameof(Core.Api.GraphQL.Models.Project.id)).EqualTo(Project.id));
    Assert.That(res.invitedTeam, Has.Count.EqualTo(1));
    Assert.That(res.invitedTeam[0].user.id, Is.EqualTo(TargetUser.id));

    _createdInvite = res.invitedTeam[0];
  }

  [Test]
  public async Task ProjectInviteCreate_By_UserId()
  {
    ProjectInviteCreateInput input = new(null, null, null, TargetUser.id);
    var res = await Sut.Create(Project.id, input);

    Assert.That(res, Has.Property(nameof(Core.Api.GraphQL.Models.Project.id)).EqualTo(Project.id));
    Assert.That(res.invitedTeam, Has.Count.EqualTo(1));
    Assert.That(res.invitedTeam[0].user.id, Is.EqualTo(TargetUser.id));
  }

  [Test]
  public void ProjectInviteCreate_InvalidInput()
  {
    Assert.CatchAsync<SpeckleGraphQLException>(async () =>
    {
      var input = new ProjectInviteCreateInput(null, null, null, null);
      await Sut.Create(Project.id, input);
    });

    Assert.CatchAsync<SpeckleGraphQLException>(async () =>
    {
      var input = new ProjectInviteCreateInput(null, "something", "something", null);
      await Sut.Create(Project.id, input);
    });
  }

  [Test]
  public async Task ProjectInviteGet()
  {
    await ProjectInviteCreate_By_Email();
    PendingStreamCollaborator collaborator = await Sut.Get(Project.id, _createdInvite.token);

    Assert.That(collaborator, Has.Property(nameof(PendingStreamCollaborator.id)).EqualTo(_createdInvite.id));
    Assert.That(
      collaborator,
      Has.Property(nameof(PendingStreamCollaborator.inviteId)).EqualTo(_createdInvite.inviteId)
    );
  }

  [Test]
  public async Task ProjectInviteUse()
  {
    await ProjectInviteCreate_By_Email();
    ProjectInviteUseInput input = new(true, _createdInvite.streamId, _createdInvite.token);
    var res = await Sut.Use(input);

    Assert.That(res, Is.True);
  }
}
