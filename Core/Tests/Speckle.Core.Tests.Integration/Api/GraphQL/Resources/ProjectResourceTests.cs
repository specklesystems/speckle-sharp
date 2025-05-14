using Speckle.Core.Api;
using Speckle.Core.Api.GraphQL.Enums;
using Speckle.Core.Api.GraphQL.Inputs;
using Speckle.Core.Api.GraphQL.Models;
using Speckle.Core.Api.GraphQL.Resources;

namespace Speckle.Core.Tests.Integration.API.GraphQL.Resources;

[TestOf(typeof(ProjectResource))]
public class ProjectResourceTests
{
  private Client _testUser;
  private Project _testProject;
  private ProjectResource Sut => _testUser.Project;

  [OneTimeSetUp]
  public async Task Setup()
  {
    _testUser = await Fixtures.SeedUserWithClient();
    _testProject = await _testUser.Project.Create(new("test project123", "desc", null));
  }

  [TestCase("Very private project", "My secret project", ProjectVisibility.Private)]
  [TestCase("Very public project", null, ProjectVisibility.Public)]
  public async Task ProjectCreate(string name, string desc, ProjectVisibility visibility)
  {
    ProjectCreateInput input = new(name, desc, visibility);
    Project result = await Sut.Create(input);
    Assert.That(result, Is.Not.Null);
    Assert.That(result, Has.Property(nameof(Project.id)).Not.Null);
    Assert.That(result, Has.Property(nameof(Project.name)).EqualTo(input.name));
    Assert.That(result, Has.Property(nameof(Project.description)).EqualTo(input.description ?? string.Empty));
    Assert.That(result, Has.Property(nameof(Project.visibility)).EqualTo(input.visibility));
  }

  [Test]
  public async Task ProjectGet()
  {
    Project result = await Sut.Get(_testProject.id);

    Assert.That(result.id, Is.EqualTo(_testProject.id));
    Assert.That(result.name, Is.EqualTo(_testProject.name));
    Assert.That(result.description, Is.EqualTo(_testProject.description));
    Assert.That(result.visibility, Is.EqualTo(_testProject.visibility));
    Assert.That(result.createdAt, Is.EqualTo(_testProject.createdAt));
  }

  [Test]
  public async Task ProjectUpdate()
  {
    const string NEW_NAME = "MY new name";
    const string NEW_DESCRIPTION = "MY new desc";
    const ProjectVisibility NEW_VISIBILITY = ProjectVisibility.Public;

    Project newProject = await Sut.Update(new(_testProject.id, NEW_NAME, NEW_DESCRIPTION, null, NEW_VISIBILITY));

    Assert.That(newProject.id, Is.EqualTo(_testProject.id));
    Assert.That(newProject.name, Is.EqualTo(NEW_NAME));
    Assert.That(newProject.description, Is.EqualTo(NEW_DESCRIPTION));
    Assert.That(newProject.visibility, Is.EqualTo(NEW_VISIBILITY));
  }

  [Test]
  public async Task ProjectDelete()
  {
    Project toDelete = await Sut.Create(new("Delete me", null, null));
    bool response = await Sut.Delete(toDelete.id);
    Assert.That(response, Is.True);

    Assert.ThrowsAsync<SpeckleGraphQLStreamNotFoundException>(async () => _ = await Sut.Get(toDelete.id));
  }
}
