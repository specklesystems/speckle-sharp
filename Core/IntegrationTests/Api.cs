using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Models;
using Speckle.Core.Transports;
using TestsUnit;

namespace TestsIntegration;

public class Api
{
  private string branchId = "";
  private string branchName = "";
  private string commitId = "";

  public Account firstUserAccount,
    secondUserAccount;

  public Client myClient,
    secondClient;

  public ServerTransport myServerTransport,
    otherServerTransport;

  private string objectId = "";

  private string streamId = "";

  [OneTimeSetUp]
  public async Task Setup()
  {
    firstUserAccount = await Fixtures.SeedUser().ConfigureAwait(false);
    secondUserAccount = await Fixtures.SeedUser().ConfigureAwait(false);

    myClient = new Client(firstUserAccount);
    secondClient = new Client(secondUserAccount);
    myServerTransport = new ServerTransport(firstUserAccount, null);
    myServerTransport.Api.CompressPayloads = false;
    otherServerTransport = new ServerTransport(firstUserAccount, null);
    otherServerTransport.Api.CompressPayloads = false;
  }

  [Test]
  public async Task ActiveUserGet()
  {
    var res = await myClient.ActiveUserGet().ConfigureAwait(false);
    Assert.That(myClient.Account.userInfo.id, Is.EqualTo(res.id));
  }

  [Test]
  public async Task OtherUserGet()
  {
    var res = await myClient.OtherUserGet(secondUserAccount.userInfo.id).ConfigureAwait(false);
    Assert.That(secondUserAccount.userInfo.name, Is.EqualTo(res.name));
  }

  [Test]
  public async Task UserSearch()
  {
    var res = await myClient.UserSearch(firstUserAccount.userInfo.email).ConfigureAwait(false);
    Assert.That(res.Count, Is.EqualTo(1));
    Assert.That(firstUserAccount.userInfo.id, Is.EqualTo(res[0].id));
  }

  [Test]
  public async Task ServerVersion()
  {
    var res = await myClient.GetServerVersion().ConfigureAwait(false);

    Assert.NotNull(res);
  }

  [Test, Order(0)]
  public async Task StreamCreate()
  {
    var res = await myClient
      .StreamCreate(new StreamCreateInput { description = "Hello World", name = "Super Stream 01" })
      .ConfigureAwait(false);

    myServerTransport.StreamId = res;
    otherServerTransport.StreamId = res;
    Assert.NotNull(res);
    streamId = res;
  }

  [Test, Order(10)]
  public async Task StreamsGet()
  {
    var res = await myClient.StreamsGet().ConfigureAwait(false);

    Assert.NotNull(res);
  }

  [Test, Order(11)]
  public async Task StreamGet()
  {
    var res = await myClient.StreamGet(streamId).ConfigureAwait(false);

    Assert.NotNull(res);
    Assert.That(res.branches.items[0].name, Is.EqualTo("main"));
    Assert.IsNotEmpty(res.collaborators);
  }

  [Test, Order(12)]
  public async Task IsStreamAccessible()
  {
    var res = await myClient.IsStreamAccessible(streamId).ConfigureAwait(false);

    Assert.True(res);
  }

  [Test, Order(13)]
  public async Task StreamSearch()
  {
    var res = await myClient.StreamSearch(streamId).ConfigureAwait(false);

    Assert.NotNull(res);
  }

  [Test, Order(20)]
  public async Task StreamUpdate()
  {
    var res = await myClient
      .StreamUpdate(
        new StreamUpdateInput
        {
          id = streamId,
          description = "Hello World",
          name = "Super Stream 01 EDITED"
        }
      )
      .ConfigureAwait(false);

    Assert.IsTrue(res);
  }

  [Test, Order(31)]
  public async Task StreamInviteCreate()
  {
    var res = await myClient
      .StreamInviteCreate(
        new StreamInviteCreateInput
        {
          streamId = streamId,
          email = secondUserAccount.userInfo.email,
          message = "Whasssup!"
        }
      )
      .ConfigureAwait(false);

    Assert.IsTrue(res);

    Assert.ThrowsAsync<ArgumentException>(
      async () =>
        await myClient.StreamInviteCreate(new StreamInviteCreateInput { streamId = streamId }).ConfigureAwait(false)
    );
  }

  [Test, Order(32)]
  public async Task StreamInviteGet()
  {
    var invites = await secondClient.GetAllPendingInvites().ConfigureAwait(false);

    Assert.NotNull(invites);
  }

  [Test, Order(33)]
  public async Task StreamInviteUse()
  {
    var invites = await secondClient.GetAllPendingInvites().ConfigureAwait(false);

    var res = await secondClient.StreamInviteUse(invites[0].streamId, invites[0].token).ConfigureAwait(false);

    Assert.IsTrue(res);
  }

  [Test, Order(34)]
  public async Task StreamUpdatePermission()
  {
    var res = await myClient
      .StreamUpdatePermission(
        new StreamPermissionInput
        {
          role = "stream:reviewer",
          streamId = streamId,
          userId = secondUserAccount.userInfo.id
        }
      )
      .ConfigureAwait(false);

    Assert.IsTrue(res);
  }

  [Test, Order(40)]
  public async Task StreamRevokePermission()
  {
    var res = await myClient
      .StreamRevokePermission(
        new StreamRevokePermissionInput { streamId = streamId, userId = secondUserAccount.userInfo.id }
      )
      .ConfigureAwait(false);

    Assert.IsTrue(res);
  }

  #region activity

  [Test, Order(51)]
  public async Task StreamGetActivity()
  {
    var res = await myClient.StreamGetActivity(streamId).ConfigureAwait(false);

    Assert.NotNull(res);
    //Assert.AreEqual(commitId, res[0].);
  }

  #endregion

  #region comments

  [Test, Order(52)]
  public async Task StreamGetComments()
  {
    var res = await myClient.StreamGetActivity(streamId).ConfigureAwait(false);

    Assert.NotNull(res);
    //Assert.AreEqual(commitId, res[0].);
  }

  #endregion

  [Test, Order(60)]
  public async Task StreamDelete()
  {
    var res = await myClient.StreamDelete(streamId).ConfigureAwait(false);
    Assert.IsTrue(res);
  }

  #region branches

  [Test, Order(41)]
  public async Task BranchCreate()
  {
    var res = await myClient
      .BranchCreate(
        new BranchCreateInput
        {
          streamId = streamId,
          description = "this is a sample branch",
          name = "sample-branch"
        }
      )
      .ConfigureAwait(false);
    Assert.NotNull(res);
    branchId = res;
    branchName = "sample-branch";
  }

  [Test, Order(42)]
  public async Task BranchGet()
  {
    var res = await myClient.BranchGet(streamId, branchName).ConfigureAwait(false);

    Assert.NotNull(res);
    Assert.That(res.description, Is.EqualTo("this is a sample branch"));
  }

  [Test, Order(43)]
  public async Task StreamGetBranches()
  {
    var res = await myClient.StreamGetBranches(streamId).ConfigureAwait(false);

    Assert.NotNull(res);
    // Branches are now returned in order of creation so 'main' should always go first.
    Assert.That(res[0].name, Is.EqualTo("main"));
  }

  [Test, Order(51)]
  public async Task StreamGetBranches_Throws_WhenRequestingOverLimit()
  {
    Assert.ThrowsAsync<SpeckleGraphQLException<StreamData>>(
      async () => await myClient.StreamGetBranches(streamId, ServerLimits.BRANCH_GET_LIMIT + 1).ConfigureAwait(false)
    );
    var res = await myClient.StreamGetBranches(streamId, ServerLimits.BRANCH_GET_LIMIT).ConfigureAwait(false);

    Assert.That(res, Is.Not.Null);
  }

  [Test, Order(52)]
  public async Task StreamGetBranches_WithManyBranches()
  {
    var newStreamId = await myClient.StreamCreate(new StreamCreateInput { name = "Many branches stream" });

    await CreateEmptyBranches(myClient, newStreamId, ServerLimits.BRANCH_GET_LIMIT);

    var res = await myClient.StreamGetBranches(newStreamId, ServerLimits.BRANCH_GET_LIMIT);

    Assert.That(res, Is.Not.Null);
    Assert.That(res, Has.Count.EqualTo(ServerLimits.BRANCH_GET_LIMIT));
  }

  public async Task CreateEmptyBranches(
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
      ptsList.Add(new Point(i, i, i));

    myObject["@Points"] = ptsList;

    bool sendError = false;
    objectId = await Operations
      .Send(
        myObject,
        new List<ITransport> { myServerTransport },
        false,
        disposeTransports: true,
        onErrorAction: (s, e) =>
        {
          sendError = true;
        }
      )
      .ConfigureAwait(false);
    Assert.IsFalse(sendError);

    var res = await myClient
      .CommitCreate(
        new CommitCreateInput
        {
          streamId = streamId,
          branchName = branchName,
          objectId = objectId,
          message = "Fibber Fibbo",
          sourceApplication = "Tests",
          totalChildrenCount = 100
        }
      )
      .ConfigureAwait(false);

    Assert.NotNull(res);
    commitId = res;

    var res2 = await myClient
      .CommitCreate(
        new CommitCreateInput
        {
          streamId = streamId,
          branchName = branchName,
          objectId = objectId,
          message = "Fabber Fabbo",
          sourceApplication = "Tests",
          totalChildrenCount = 100,
          parents = new List<string> { commitId }
        }
      )
      .ConfigureAwait(false);

    Assert.NotNull(res2);
    commitId = res2;
  }

  [Test, Order(44)]
  public async Task CommitGet()
  {
    var res = await myClient.CommitGet(streamId, commitId).ConfigureAwait(false);

    Assert.NotNull(res);
    Assert.That(res.message, Is.EqualTo("Fabber Fabbo"));
  }

  [Test, Order(45)]
  public async Task StreamGetCommits()
  {
    var res = await myClient.StreamGetCommits(streamId).ConfigureAwait(false);

    Assert.NotNull(res);
    Assert.That(res[0].id, Is.EqualTo(commitId));
  }

  #region object

  [Test, Order(45)]
  public async Task ObjectGet()
  {
    var res = await myClient.ObjectGet(streamId, objectId).ConfigureAwait(false);

    Assert.NotNull(res);
    Assert.That(res.totalChildrenCount, Is.EqualTo(100));
  }

  #endregion

  [Test, Order(46)]
  public async Task CommitUpdate()
  {
    var res = await myClient
      .CommitUpdate(
        new CommitUpdateInput
        {
          streamId = streamId,
          id = commitId,
          message = "DIM IS DA BEST"
        }
      )
      .ConfigureAwait(false);

    Assert.IsTrue(res);
  }

  [Test, Order(47)]
  public async Task CommitReceived()
  {
    var res = await myClient
      .CommitReceived(
        new CommitReceivedInput
        {
          commitId = commitId,
          streamId = streamId,
          sourceApplication = "sharp-tests",
          message = "The test message"
        }
      )
      .ConfigureAwait(false);

    Assert.IsTrue(res);
  }

  [Test, Order(48)]
  public async Task CommitDelete()
  {
    var res = await myClient
      .CommitDelete(new CommitDeleteInput { id = commitId, streamId = streamId })
      .ConfigureAwait(false);
    Assert.IsTrue(res);
  }

  #endregion


  [Test, Order(49)]
  public async Task BranchUpdate()
  {
    var res = await myClient
      .BranchUpdate(
        new BranchUpdateInput
        {
          streamId = streamId,
          id = branchId,
          name = "sample-branch EDITED"
        }
      )
      .ConfigureAwait(false);

    Assert.IsTrue(res);
  }

  [Test, Order(50)]
  public async Task BranchDelete()
  {
    var res = await myClient
      .BranchDelete(new BranchDeleteInput { id = branchId, streamId = streamId })
      .ConfigureAwait(false);
    Assert.IsTrue(res);
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
}
