using Speckle.Core.Api;
using Speckle.Core.Api.GraphQL.Enums;
using Speckle.Core.Api.GraphQL.Inputs;
using Speckle.Core.Api.GraphQL.Models;
using Speckle.Core.Api.GraphQL.Models.Responses;
using Speckle.Core.Api.GraphQL.Resources;

namespace Speckle.Core.Tests.Integration.API.GraphQL.Resources;

[TestOf(typeof(ProjectResource))]
public class ProjectResourceExceptionalTests
{
  private ProjectResource Sut => ResourcesTestsFixture.FirstUser.Project;
  private Client UnauthedUser => ResourcesTestsFixture.UnauthedUser;
  private Client SecondUser => ResourcesTestsFixture.SecondUser;
  private Project testProject => ResourcesTestsFixture.Project;

  //TODO: There's got to be a smarter way to parametrise this.
  //We want to check the following cases
  // 1. User lacks permissions
  // 2. Target (Project or user) doesn't exist)
  // 3. Cancellation
  // 4. Server doesn't exist (is down)

  [Test]
  public void ProjectCreate_WithoutAuth()
  {
    ProjectCreateInput input =
      new("The best project", "The best description for the best project", ProjectVisibility.Private);

    Assert.ThrowsAsync<Exception>(async () => await UnauthedUser.Project.Create(input));
  }

  [Test, Order(10)]
  public async Task ProjectGet_WithoutAuth()
  {
    ProjectCreateInput input = new("Private STream", "A very private stream", ProjectVisibility.Private);

    Project privateStream = await Sut.Create(input);

    Assert.ThrowsAsync<Exception>(async () => await UnauthedUser.Project.Get(privateStream.id));
  }

  [Test]
  public void ProjectGet_NonExistentProject()
  {
    Assert.ThrowsAsync<SpeckleGraphQLException<ProjectResponse>>(async () => await Sut.Get("NonExistentProject"));
  }

  [Test]
  public void ProjectUpdate_NonExistentProject()
  {
    Assert.ThrowsAsync<Exception>(async () => _ = await Sut.Update(new("NonExistentProject", "My new name")));
  }

  [Test, Order(20)]
  public void ProjectUpdate_NoAuth()
  {
    Assert.ThrowsAsync<Exception>(
      async () => _ = await UnauthedUser.Project.Update(new(testProject.id, "My new name"))
    );
  }

  [Test]
  [TestCase("stream:owner")]
  [TestCase("stream:reviewer")]
  [TestCase(null)]
  public void ProjectUpdateRole_NonExistentProject(string newRole)
  {
    ProjectUpdateRoleInput input = new(SecondUser.Account.id, "NonExistentProject", newRole);

    Assert.ThrowsAsync<Exception>(async () => await Sut.UpdateRole(input));
  }

  [Test]
  [TestCase("stream:owner")]
  [TestCase("stream:reviewer")]
  [TestCase(null)]
  public void ProjectUpdateRole_NonAuth(string newRole)
  {
    ProjectUpdateRoleInput input = new(SecondUser.Account.id, "NonExistentProject", newRole);

    Assert.ThrowsAsync<Exception>(async () => await UnauthedUser.Project.UpdateRole(input));
  }

  [Test, Order(100)]
  public async Task ProjectDelete_NonExistentProject()
  {
    bool response = await Sut.Delete(testProject.id);
    Assert.That(response, Is.True);

    Assert.ThrowsAsync<Exception>(async () => _ = await Sut.Get(testProject.id)); //TODO: Exception types
  }
}
