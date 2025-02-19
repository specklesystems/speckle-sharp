using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Models;
using Speckle.Core.Tests.Unit.Kits;
using Speckle.Core.Transports;

namespace Speckle.Core.Tests.Integration.Api.GraphQL.Legacy;

public class LegacyAPITests : IDisposable
{
  private string _branchId = "";
  private string _branchName = "";
  private string _commitId = "";

  private Account _firstUserAccount,
    _secondUserAccount;

  private Client _myClient,
    _secondClient;

  private ServerTransport _myServerTransport,
    _otherServerTransport;

  private string _objectId = "";

  private string _streamId = "";

  [OneTimeSetUp]
  public async Task Setup()
  {
    _firstUserAccount = await Fixtures.SeedUser();
    _secondUserAccount = await Fixtures.SeedUser();

    _myClient = new Client(_firstUserAccount);
    _secondClient = new Client(_secondUserAccount);
  }

  private void InitServerTransport()
  {
    _myServerTransport = new ServerTransport(_firstUserAccount, _streamId);
    _myServerTransport.Api.CompressPayloads = false;
    _otherServerTransport = new ServerTransport(_firstUserAccount, _streamId);
    _otherServerTransport.Api.CompressPayloads = false;
  }

  [Test]
  public async Task ActiveUserGet()
  {
    var res = await _myClient.ActiveUserGet();
    Assert.That(res.id, Is.EqualTo(_myClient.Account.userInfo.id));
  }

  [Test]
  public async Task OtherUserGet()
  {
    var res = await _myClient.OtherUserGet(_secondUserAccount.userInfo.id);
    Assert.That(res!.name, Is.EqualTo(_secondUserAccount.userInfo.name));
  }

  [Test]
  public async Task UserSearch()
  {
    var res = await _myClient.UserSearch(_firstUserAccount.userInfo.email);
    Assert.That(res, Has.Count.EqualTo(1));
    Assert.That(res[0].id, Is.EqualTo(_firstUserAccount.userInfo.id));
  }

  [Test]
  public async Task ServerVersion()
  {
    var res = await _myClient.GetServerVersion();

    Assert.That(res, Is.Not.Null);
  }

  [Test, Order(0)]
  public async Task StreamCreate()
  {
    var res = await _myClient.StreamCreate(
      new StreamCreateInput { description = "Hello World", name = "Super Stream 01" }
    );

    Assert.That(res, Is.Not.Null);
    _streamId = res;
    InitServerTransport();
  }

  [Test, Order(10)]
  public async Task StreamsGet()
  {
    var res = await _myClient.StreamsGet();

    Assert.That(res, Is.Not.Null);
  }

  [Test, Order(11)]
  public async Task StreamGet()
  {
    var res = await _myClient.StreamGet(_streamId);

    Assert.That(res, Is.Not.Null);
    Assert.That(res.branches.items[0].name, Is.EqualTo("main"));
    Assert.That(res.collaborators, Is.Not.Empty);
  }

  [Test, Order(12)]
  public async Task IsStreamAccessible()
  {
    var res = await _myClient.IsStreamAccessible(_streamId);

    Assert.That(res, Is.True);
  }

  [Test, Order(13)]
  public async Task StreamSearch()
  {
    var res = await _myClient.StreamSearch(_streamId);

    Assert.That(res, Is.Not.Null);
  }

  [Test, Order(20)]
  public async Task StreamUpdate()
  {
    var res = await _myClient.StreamUpdate(
      new StreamUpdateInput
      {
        id = _streamId,
        description = "Hello World",
        name = "Super Stream 01 EDITED"
      }
    );

    Assert.That(res, Is.True);
  }

  // [Test, Order(31)]
  // public async Task StreamInviteCreate()
  // {
  //   var res = await _myClient.StreamInviteCreate(
  //     new StreamInviteCreateInput
  //     {
  //       streamId = _streamId,
  //       email = _secondUserAccount.userInfo.email,
  //       message = "Whasssup!"
  //     }
  //   );
  //
  //   Assert.That(res, Is.True);
  //
  //   Assert.ThrowsAsync<ArgumentException>(
  //     async () => await _myClient.StreamInviteCreate(new StreamInviteCreateInput { streamId = _streamId })
  //   );
  // }

  // [Test, Order(32)]
  // public async Task StreamInviteGet()
  // {
  //   var invites = await _secondClient.GetAllPendingInvites();
  //
  //   Assert.That(invites, Is.Not.Null);
  // }
  //
  // [Test, Order(33)]
  // public async Task StreamInviteUse()
  // {
  //   var invites = await _secondClient.GetAllPendingInvites();
  //
  //   var res = await _secondClient.StreamInviteUse(invites[0].streamId, invites[0].token);
  //
  //   Assert.That(res, Is.True);
  // }

  // [Test, Order(34)]
  // public async Task StreamUpdatePermission()
  // {
  //   var res = await _myClient.StreamUpdatePermission(
  //     new StreamPermissionInput
  //     {
  //       role = StreamRoles.STREAM_REVIEWER,
  //       streamId = _streamId,
  //       userId = _secondUserAccount.userInfo.id
  //     }
  //   );
  //
  //   Assert.That(res, Is.True);
  // }
  //
  // [Test, Order(40)]
  // public async Task StreamRevokePermission()
  // {
  //   var res = await _myClient.StreamRevokePermission(
  //     new StreamRevokePermissionInput { streamId = _streamId, userId = _secondUserAccount.userInfo.id }
  //   );
  //
  //   Assert.That(res, Is.True);
  // }

  // #region activity
  //
  // [Test, Order(51)]
  // public async Task StreamGetActivity()
  // {
  //   var res = await _myClient.StreamGetActivity(_streamId);
  //
  //   Assert.That(res, Is.Not.Null);
  //   //Assert.AreEqual(commitId, res[0].);
  // }
  //
  // #endregion

  #region comments

  // [Test, Order(52)]
  // public async Task StreamGetComments()
  // {
  //   var res = await _myClient.StreamGetActivity(_streamId);
  //
  //   Assert.That(res, Is.Not.Null);
  //   //Assert.AreEqual(commitId, res[0].);
  // }

  #endregion

  [Test, Order(60)]
  public async Task StreamDelete()
  {
    var res = await _myClient.StreamDelete(_streamId);
    Assert.That(res, Is.True);
  }

  #region branches

  [Test, Order(41)]
  public async Task BranchCreate()
  {
    var res = await _myClient.BranchCreate(
      new BranchCreateInput
      {
        streamId = _streamId,
        description = "this is a sample branch",
        name = "sample-branch"
      }
    );
    Assert.That(res, Is.Not.Null);
    _branchId = res;
    _branchName = "sample-branch";
  }

  [Test, Order(42)]
  public async Task BranchGet()
  {
    var res = await _myClient.BranchGet(_streamId, _branchName);

    Assert.That(res, Is.Not.Null);
    Assert.That(res.description, Is.EqualTo("this is a sample branch"));
  }

  [Test, Order(43)]
  public async Task StreamGetBranches()
  {
    var res = await _myClient.StreamGetBranches(_streamId);

    Assert.That(res, Is.Not.Null);
    // Branches are now returned in order of creation so 'main' should always go first.
    Assert.That(res[0].name, Is.EqualTo("main"));
  }

  [Test, Order(51)]
  public async Task StreamGetBranches_Throws_WhenRequestingOverLimit()
  {
    Assert.ThrowsAsync<SpeckleGraphQLException<StreamData>>(
      async () => await _myClient.StreamGetBranches(_streamId, ServerLimits.BRANCH_GET_LIMIT + 1)
    );
    var res = await _myClient.StreamGetBranches(_streamId, ServerLimits.BRANCH_GET_LIMIT);

    Assert.That(res, Is.Not.Null);
  }

  [Test, Order(52)]
  public async Task StreamGetBranches_WithManyBranches()
  {
    var newStreamId = await _myClient.StreamCreate(new StreamCreateInput { name = "Many branches stream" });

    await CreateEmptyBranches(_myClient, newStreamId, ServerLimits.BRANCH_GET_LIMIT);

    var res = await _myClient.StreamGetBranches(newStreamId, ServerLimits.BRANCH_GET_LIMIT);

    Assert.That(res, Is.Not.Null);
    Assert.That(res, Has.Count.EqualTo(ServerLimits.BRANCH_GET_LIMIT));
  }

  private async Task CreateEmptyBranches(
    Client client,
    string streamId,
    int branchCount,
    string branchPrefix = "Test branch"
  )
  {
    // now let's send HTTP requests to each of these URLs in parallel
    var options = new ParallelOptions { MaxDegreeOfParallelism = 2 };

    // now let's send HTTP requests to each of these URLs in parallel
    await Parallel.ForEachAsync(
      Enumerable.Range(0, branchCount),
      options,
      async (i, cancellationToken) =>
      {
        await client.BranchCreate(
          new BranchCreateInput { name = $"{branchPrefix} {i}", streamId = streamId },
          cancellationToken
        );
      }
    );
  }

  #region commit

  [Test, Order(43)]
  public async Task CommitCreate()
  {
    var myObject = new Base();
    var ptsList = new List<Point>();
    for (int i = 0; i < 100; i++)
    {
      ptsList.Add(new Point(i, i, i));
    }

    myObject["@Points"] = ptsList;

    _objectId = await Operations.Send(myObject, new List<ITransport> { _myServerTransport });

    Assert.That(_objectId, Is.Not.Null);

    var res = await _myClient.CommitCreate(
      new CommitCreateInput
      {
        streamId = _streamId,
        branchName = _branchName,
        objectId = _objectId,
        message = "Fibber Fibbo",
        sourceApplication = "Tests",
        totalChildrenCount = 100
      }
    );

    Assert.That(res, Is.Not.Null);
    _commitId = res;

    var res2 = await _myClient.CommitCreate(
      new CommitCreateInput
      {
        streamId = _streamId,
        branchName = _branchName,
        objectId = _objectId,
        message = "Fabber Fabbo",
        sourceApplication = "Tests",
        totalChildrenCount = 100,
        parents = new List<string> { _commitId }
      }
    );

    Assert.That(res2, Is.Not.Null);
    _commitId = res2;
  }

  [Test, Order(44)]
  public async Task CommitGet()
  {
    var res = await _myClient.CommitGet(_streamId, _commitId);

    Assert.That(res, Is.Not.Null);
    Assert.That(res.message, Is.EqualTo("Fabber Fabbo"));
  }

  [Test, Order(45)]
  public async Task StreamGetCommits()
  {
    var res = await _myClient.StreamGetCommits(_streamId);

    Assert.That(res, Is.Not.Null);
    Assert.That(res[0].id, Is.EqualTo(_commitId));
  }

  #region object

  [Test, Order(45)]
  public async Task ObjectGet()
  {
    var res = await _myClient.ObjectGet(_streamId, _objectId);

    Assert.That(res, Is.Not.Null);
    // Assert.That(res.totalChildrenCount, Is.EqualTo(100));
  }

  #endregion

  [Test, Order(46)]
  public async Task CommitUpdate()
  {
    var res = await _myClient.CommitUpdate(
      new CommitUpdateInput
      {
        streamId = _streamId,
        id = _commitId,
        message = "DIM IS DA BEST"
      }
    );

    Assert.That(res, Is.True);
  }

  [Test, Order(47)]
  public async Task CommitReceived()
  {
    var res = await _myClient.CommitReceived(
      new CommitReceivedInput
      {
        commitId = _commitId,
        streamId = _streamId,
        sourceApplication = "sharp-tests",
        message = "The test message"
      }
    );

    Assert.That(res, Is.True);
  }

  [Test, Order(48)]
  public async Task CommitDelete()
  {
    var res = await _myClient.CommitDelete(new CommitDeleteInput { id = _commitId, streamId = _streamId });
    Assert.That(res, Is.True);
  }

  #endregion


  [Test, Order(49)]
  public async Task BranchUpdate()
  {
    var res = await _myClient.BranchUpdate(
      new BranchUpdateInput
      {
        streamId = _streamId,
        id = _branchId,
        name = "sample-branch EDITED"
      }
    );

    Assert.That(res, Is.True);
  }

  [Test, Order(50)]
  public async Task BranchDelete()
  {
    var res = await _myClient.BranchDelete(new BranchDeleteInput { id = _branchId, streamId = _streamId });
    Assert.That(res, Is.True);
  }

  #endregion

  #region send/receive bare

  //[Test, Order(60)]
  //public async Task SendDetached()
  //{
  //  var myObject = new Base();
  //  var ptsList = new List<Point>();
  //  for (int i = 0; i < 100; i++)
  //    ptsList.Add(new Point(i, i, i));

  //  myObject["@Points"] = ptsList;

  //  var otherTransport = new ServerTransport(firstUserAccount, null);
  //  otherTransport.StreamId =

  //  objectId = await Operations.Send(myObject, new List<ITransport>() { myServerTransport }, disposeTransports: true);
  //}

  //[Test, Order(61)]
  //public async Task ReceiveAndCompose()
  //{
  //  var myObject = await Operations.Receive(objectId, myServerTransport);
  //  Assert.NotNull(myObject);
  //  Assert.AreEqual(100, ((List<object>)myObject["@Points"]).Count);
  //}

  #endregion

  public void Dispose()
  {
    _myClient?.Dispose();
    _secondClient?.Dispose();
    _myServerTransport?.Dispose();
    _otherServerTransport?.Dispose();
  }
}
