using Speckle.Core.Api;
using Speckle.Core.Api.GraphQL;
using Speckle.Core.Api.GraphQL.Inputs;
using Speckle.Core.Api.GraphQL.Models;
using Speckle.Core.Api.GraphQL.Resources;

namespace Speckle.Core.Tests.Integration.API.GraphQL.Resources;

[TestOf(typeof(ProjectInviteResource))]
public class ProjectInviteResourceTests
{
  private Client _inviter,
    _invitee;
  private Project _project;
  private PendingStreamCollaborator _createdInvite;

  [SetUp]
  public async Task Setup()
  {
    _inviter = await Fixtures.SeedUserWithClient();
    _invitee = await Fixtures.SeedUserWithClient();
    _project = await _inviter.Project.Create(new("test", null, null));
    _createdInvite = await SeedInvite();
  }

  private async Task<PendingStreamCollaborator> SeedInvite()
  {
    ProjectInviteCreateInput input = new(_invitee.Account.userInfo.email, null, null, null);
    var res = await _inviter.ProjectInvite.Create(_project.id, input);
    var invites = await _invitee.ActiveUser.ProjectInvites();
    return invites.First(i => i.projectId == res.id);
  }

  [Test]
  public async Task ProjectInviteCreate_ByEmail()
  {
    ProjectInviteCreateInput input = new(_invitee.Account.userInfo.email, null, null, null);
    var res = await _inviter.ProjectInvite.Create(_project.id, input);

    var invites = await _invitee.ActiveUser.ProjectInvites();
    var invite = invites.First(i => i.projectId == res.id);

    Assert.That(res, Has.Property(nameof(_project.id)).EqualTo(_project.id));
    Assert.That(res.invitedTeam, Has.Count.EqualTo(1));
    Assert.That(invite.user.id, Is.EqualTo(_invitee.Account.userInfo.id));
    Assert.That(invite.token, Is.Not.Null);
  }

  [Test]
  public async Task ProjectInviteCreate_ByUserId()
  {
    ProjectInviteCreateInput input = new(null, null, null, _invitee.Account.userInfo.id);
    var res = await _inviter.ProjectInvite.Create(_project.id, input);

    Assert.That(res, Has.Property(nameof(_project.id)).EqualTo(_project.id));
    Assert.That(res.invitedTeam, Has.Count.EqualTo(1));
    Assert.That(res.invitedTeam[0].user.id, Is.EqualTo(_invitee.Account.userInfo.id));
  }

  [Test]
  public async Task ProjectInviteGet()
  {
    var collaborator = await _invitee.ProjectInvite.Get(_project.id, _createdInvite.token);

    Assert.That(
      collaborator,
      Has.Property(nameof(PendingStreamCollaborator.inviteId)).EqualTo(_createdInvite.inviteId)
    );
    Assert.That(collaborator.user.id, Is.EqualTo(_createdInvite.user.id));
  }

  [Test]
  public async Task ProjectInviteUse_MemberAdded()
  {
    ProjectInviteUseInput input = new(true, _createdInvite.projectId, _createdInvite.token);
    var res = await _invitee.ProjectInvite.Use(input);
    Assert.That(res, Is.True);

    var project = await _inviter.Project.GetWithTeam(_project.id);
    var teamMembers = project.team.Select(c => c.user.id);
    var expectedTeamMembers = new[] { _inviter.Account.userInfo.id, _invitee.Account.userInfo.id };
    Assert.That(teamMembers, Is.EquivalentTo(expectedTeamMembers));
  }

  [Test]
  public async Task ProjectInviteCancel_MemberNotAdded()
  {
    var res = await _inviter.ProjectInvite.Cancel(_createdInvite.projectId, _createdInvite.inviteId);

    Assert.That(res.invitedTeam, Is.Empty);
  }

  [Test]
  [TestCase(StreamRoles.STREAM_OWNER)]
  [TestCase(StreamRoles.STREAM_REVIEWER)]
  [TestCase(StreamRoles.STREAM_CONTRIBUTOR)]
  [TestCase(StreamRoles.REVOKE)]
  public async Task ProjectUpdateRole(string newRole)
  {
    await ProjectInviteUse_MemberAdded();
    ProjectUpdateRoleInput input = new(_invitee.Account.userInfo.id, _project.id, newRole);
    _ = await _inviter.Project.UpdateRole(input);

    Project finalProject = await _invitee.Project.Get(_project.id);
    Assert.That(finalProject.role, Is.EqualTo(newRole));
  }
}
