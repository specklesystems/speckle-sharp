using NUnit.Framework;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using GraphQL;
using System.Diagnostics;

namespace Tests
{
  public class GraphQLClientTests
  {
    private Client _client;

    [OneTimeSetUp]
    public void Setup()
    {
      _client = new Client(
        new Account
        {
          token = "this is a scam",
          serverInfo = new ServerInfo { url = "http://goto.testing" }
        }
      );
    }

    class FakeGqlResponseModel { }

    private static IEnumerable<TestCaseData> ErrorCases()
    {
      yield return new TestCaseData(
        typeof(SpeckleGraphQLForbiddenException<FakeGqlResponseModel>),
        new Map { { "code", "FORBIDDEN" } }
      );
      yield return new TestCaseData(
        typeof(SpeckleGraphQLForbiddenException<FakeGqlResponseModel>),
        new Map { { "code", "UNAUTHENTICATED" } }
      );
      yield return new TestCaseData(
        typeof(SpeckleGraphQLInternalErrorException<FakeGqlResponseModel>),
        new Map { { "code", "INTERNAL_SERVER_ERROR" } }
      );
      yield return new TestCaseData(
        typeof(SpeckleGraphQLException<FakeGqlResponseModel>),
        new Map { { "foo", "bar" } }
      );
    }

    [Test]
    [TestCaseSource(nameof(ErrorCases))]
    public void TestExceptionThrowingFromGraphQLErrors(Type exType, Map extensions)
    {
      Assert.Throws(
        exType,
        () =>
          _client.MaybeThrowFromGraphQLErrors<FakeGqlResponseModel>(
            new GraphQLRequest(),
            new GraphQLResponse<FakeGqlResponseModel>
            {
              Errors = new GraphQLError[] { new GraphQLError { Extensions = extensions } }
            }
          )
      );
    }

    [Test]
    public void TestMaybeThrowsDoesntThrowForNoErrors()
    {
      _client.MaybeThrowFromGraphQLErrors<string>(
        new GraphQLRequest(),
        new GraphQLResponse<string>()
      );
      // We're just checking that the prev function didn't throw
      Assert.True(true);
    }

    [Test]
    public async Task TestExecuteWithResiliencePoliciesDoesntRetryTaskCancellation()
    {
      var timer = new Stopwatch();
      timer.Start();
      Assert.ThrowsAsync<TaskCanceledException>(async () =>
      {
        var tokenSource = new CancellationTokenSource();
        tokenSource.Cancel();
        await _client.ExecuteWithResiliencePolicies<string>(
          async () =>
            await Task.Run(
              async () =>
              {
                await Task.Delay(1000);
                return "foo";
              },
              tokenSource.Token
            )
        );
      });
      timer.Stop();
      var elapsed = timer.ElapsedMilliseconds;

      // the default retry policy would retry 5 times with 1 second jitter backoff each
      // if the elapsed is less than a second, this was def not retried
      Assert.Less(elapsed, 1000);
    }

    [Test]
    public async Task TestExecuteWithResiliencePoliciesRetry()
    {
      var counter = 0;
      var maxRetryCount = 5;
      var expectedResult = "finally it finishes";
      var timer = new Stopwatch();
      timer.Start();
      var result = await _client.ExecuteWithResiliencePolicies<string>(async () =>
      {
        counter++;
        if (counter < maxRetryCount)
          throw new SpeckleGraphQLInternalErrorException<string>(
            new GraphQLRequest(),
            new GraphQLResponse<string>()
          );
        return expectedResult;
      });
      timer.Stop();
      // The baseline for wait is 1 seconds between the jittered retry
      Assert.GreaterOrEqual(timer.ElapsedMilliseconds, 5000);
      Assert.AreEqual(maxRetryCount, counter);
    }
  }
}