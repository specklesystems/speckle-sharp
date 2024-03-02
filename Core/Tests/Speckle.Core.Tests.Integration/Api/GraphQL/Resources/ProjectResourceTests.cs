using Speckle.Core.Api.GraphQL.Enums;
using Speckle.Core.Api.GraphQL.Inputs;
using Speckle.Core.Api.GraphQL.Models;
using Speckle.Core.Api.GraphQL.Resources;

namespace Speckle.Core.Tests.Integration.API.GraphQL.Resources;

[TestOf(typeof(ProjectResource))]
public class ProjectResourceTests : ResourcesTests
{
  private ProjectResource Sut => FirstUser.Project;
  private Project _project;

  [Test, Order(10)]
  public async Task ProjectCreate()
  {
    ProjectCreateInput input =
      new("The best project", "The best description for the best project", ProjectVisibility.Private);
    Project result = await Sut.Create(input);

    Assert.That(result, Is.Not.Null);
    Assert.That(result, Has.Property(nameof(Project.id)).Not.Null);
    Assert.That(result, Has.Property(nameof(Project.name)).EqualTo(input.name));
    Assert.That(result, Has.Property(nameof(Project.description)).EqualTo(input.description));
    Assert.That(result, Has.Property(nameof(Project.visibility)).EqualTo(input.visibility));

    _project = result;
  }

  [Test, Order(20)]
  public async Task ProjectGet()
  {
    Project result = await Sut.Get(_project.id);

    Assert.That(result.id, Is.EqualTo(_project.id));
    Assert.That(result.name, Is.EqualTo(_project.name));
    Assert.That(result.description, Is.EqualTo(_project.description));
    Assert.That(result.visibility, Is.EqualTo(_project.visibility));
    Assert.That(result.createdAt, Is.EqualTo(_project.createdAt));
  }

  [Test, Order(30)]
  public async Task ProjectUpdate()
  {
    const string NEW_NAME = "MY new name";
    const string NEW_DESCRIPTION = "MY new name";
    const ProjectVisibility NEW_VISIBILITY = ProjectVisibility.Public;

    Project newProject = await Sut.Update(new(_project.id, NEW_NAME, NEW_DESCRIPTION, null, NEW_VISIBILITY));

    Assert.That(newProject.id, Is.EqualTo(_project.id));
    Assert.That(newProject.name, Is.EqualTo(NEW_NAME));
    Assert.That(newProject.description, Is.EqualTo(NEW_DESCRIPTION));
    Assert.That(newProject.visibility, Is.EqualTo(NEW_VISIBILITY));

    _project = newProject;
  }

  [Test, Order(40)]
  [TestCase("stream:owner")]
  [TestCase("stream:reviewer")] //TODO: be exhaustive
  [TestCase(null)] //Revoke access
  public async Task ProjectUpdateRole(string newRole)
  {
    //TODO: figure out if this test could work, we may need to invite the user first...
    ProjectUpdateRoleInput input = new(SecondUser.Account.userInfo.id, _project.id, newRole);
    _ = await Sut.UpdateRole(input);

    Project finalProject = await SecondUser.Project.Get(_project.id);
    Assert.That(finalProject.role, Is.EqualTo(newRole));
  }

  [Test, Order(100)]
  public async Task ProjectDelete()
  {
    bool response = await Sut.Delete(_project.id);
    Assert.That(response, Is.True);

    Assert.ThrowsAsync<Exception>(async () => _ = await Sut.Get(_project.id)); //TODO: Exception types
  }
}
