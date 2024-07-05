using Speckle.Core.Api;
using Speckle.Core.Api.GraphQL.Inputs;
using Speckle.Core.Api.GraphQL.Models;
using Speckle.Core.Api.GraphQL.Resources;

namespace Speckle.Core.Tests.Integration.API.GraphQL.Resources;

[TestOf(typeof(CommentResource))]
public class CommentResourceTests
{
  private Client _testUser;
  private CommentResource Sut => _testUser.Comment;
  private Project _project;
  private Model _model;
  private string _versionId;
  private Comment _comment;

  [SetUp]
  public async Task Setup()
  {
    _testUser = await Fixtures.SeedUserWithClient();
    _project = await _testUser.Project.Create(new("Test project", "", null));
    _model = await _testUser.Model.Create(new("Test Model 1", "", _project.id));
    _versionId = await Fixtures.CreateVersion(_testUser, _project.id, _model.name);
    _comment = await CreateComment();
  }

  [Test]
  public async Task GetProjectComments()
  {
    var comments = await Sut.GetProjectComments(_project.id);
    Assert.That(comments.items.Count, Is.EqualTo(1));
    Assert.That(comments.totalCount, Is.EqualTo(1));

    Comment comment = comments.items[0];
    Assert.That(comment, Is.Not.Null);
    Assert.That(comment, Has.Property(nameof(Comment.authorId)).EqualTo(_testUser.Account.userInfo.id));

    Assert.That(comment, Has.Property(nameof(Comment.id)).EqualTo(_comment.id));
    Assert.That(comment, Has.Property(nameof(Comment.authorId)).EqualTo(_comment.authorId));
    Assert.That(comment, Has.Property(nameof(Comment.archived)).EqualTo(_comment.archived));
    Assert.That(comment, Has.Property(nameof(Comment.archived)).EqualTo(false));
    Assert.That(comment, Has.Property(nameof(Comment.createdAt)).EqualTo(_comment.createdAt));
  }

  [Test]
  public async Task MarkViewed()
  {
    var viewed = await Sut.MarkViewed(_comment.id);
    Assert.That(viewed, Is.True);
    viewed = await Sut.MarkViewed(_comment.id);
    Assert.That(viewed, Is.True);
  }

  [Test]
  public async Task Archive()
  {
    var archived = await Sut.Archive(_comment.id);
    Assert.That(archived, Is.True);

    archived = await Sut.Archive(_comment.id);
    Assert.That(archived, Is.True);
  }

  [Test]
  public async Task Edit()
  {
    var blobs = await Fixtures.SendBlobData(_testUser.Account, _project.id);
    var blobIds = blobs.Select(b => b.id).ToList();
    EditCommentInput input = new(new(blobIds, null), _comment.id);

    var editedComment = await Sut.Edit(input);

    Assert.That(editedComment, Is.Not.Null);
    Assert.That(editedComment, Has.Property(nameof(Comment.id)).EqualTo(_comment.id));
    Assert.That(editedComment, Has.Property(nameof(Comment.authorId)).EqualTo(_comment.authorId));
    Assert.That(editedComment, Has.Property(nameof(Comment.createdAt)).EqualTo(_comment.createdAt));
    Assert.That(editedComment, Has.Property(nameof(Comment.updatedAt)).GreaterThanOrEqualTo(_comment.updatedAt));
  }

  [Test]
  public async Task Reply()
  {
    var blobs = await Fixtures.SendBlobData(_testUser.Account, _project.id);
    var blobIds = blobs.Select(b => b.id).ToList();
    CreateCommentReplyInput input = new(new(blobIds, null), _comment.id);

    var editedComment = await Sut.Reply(input);

    Assert.That(editedComment, Is.Not.Null);
  }

  private async Task<Comment> CreateComment()
  {
    return await Fixtures.CreateComment(_testUser, _project.id, _model.id, _versionId);
  }
}
