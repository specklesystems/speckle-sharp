using Speckle.Core.Api;
using Speckle.Core.Api.GraphQL.Enums;
using Speckle.Core.Api.GraphQL.Inputs;
using Speckle.Core.Api.GraphQL.Models;
using Speckle.Core.Api.GraphQL.Models.Responses;
using Speckle.Core.Api.GraphQL.Resources;

namespace Speckle.Core.Tests.Integration.API.GraphQL.Resources;

[TestOf(typeof(ProjectResource))]
public class ProjectResourceTests
{
  private ProjectResource Sut => ResourcesTestsFixture.FirstUser.Project;
  private Project TestProject => ResourcesTestsFixture.Project;
  private Client TestOtherUser => ResourcesTestsFixture.SecondUser;

  [TestCase("Very private project", "My secret project", ProjectVisibility.Private)]
  [TestCase("Very public project", null, ProjectVisibility.Public)]
  public async Task ProjectCreate(string name, string desc, ProjectVisibility visibility)
  {
    ProjectCreateInput input = new(name, desc, visibility);
    Project result = await Sut.Create(input);
    Assert.That(result, Is.Not.Null);
    Assert.That(result, Has.Property(nameof(Project.id)).Not.Null);
    Assert.That(result, Has.Property(nameof(Project.name)).EqualTo(input.name));
    Assert.That(result, Has.Property(nameof(Project.description)).EqualTo(input.description));
    Assert.That(result, Has.Property(nameof(Project.visibility)).EqualTo(input.visibility));
  }

  [Test]
  public async Task ProjectGet()
  {
    Project result = await Sut.Get(TestProject.id);

    Assert.That(result.id, Is.EqualTo(TestProject.id));
    Assert.That(result.name, Is.EqualTo(TestProject.name));
    Assert.That(result.description, Is.EqualTo(TestProject.description));
    Assert.That(result.visibility, Is.EqualTo(TestProject.visibility));
    Assert.That(result.createdAt, Is.EqualTo(TestProject.createdAt));
  }

  [Test]
  public async Task ProjectUpdate()
  {
    const string NEW_NAME = "MY new name";
    const string NEW_DESCRIPTION = "MY new name";
    const ProjectVisibility NEW_VISIBILITY = ProjectVisibility.Public;

    Project newProject = await Sut.Update(new(TestProject.id, NEW_NAME, NEW_DESCRIPTION, null, NEW_VISIBILITY));

    Assert.That(newProject.id, Is.EqualTo(TestProject.id));
    Assert.That(newProject.name, Is.EqualTo(NEW_NAME));
    Assert.That(newProject.description, Is.EqualTo(NEW_DESCRIPTION));
    Assert.That(newProject.visibility, Is.EqualTo(NEW_VISIBILITY));
  }

  [Test]
  [TestCase("stream:owner")]
  [TestCase("stream:reviewer")] //TODO: be exhaustive
  [TestCase(null)] //Revoke access
  public async Task ProjectUpdateRole(string newRole)
  {
    //TODO: figure out if this test could work, we may need to invite the user first...
    ProjectUpdateRoleInput input = new(TestOtherUser.Account.userInfo.id, TestProject.id, newRole);
    _ = await Sut.UpdateRole(input);

    Project finalProject = await TestOtherUser.Project.Get(TestProject.id);
    Assert.That(finalProject.role, Is.EqualTo(newRole));
  }

  [Test]
  public async Task ProjectDelete()
  {
    bool response = await Sut.Delete(TestProject.id);
    Assert.That(response, Is.True);

    Assert.ThrowsAsync<SpeckleGraphQLException<ProjectResponse>>(async () => _ = await Sut.Get(TestProject.id)); //TODO: Exception types
  }
}
