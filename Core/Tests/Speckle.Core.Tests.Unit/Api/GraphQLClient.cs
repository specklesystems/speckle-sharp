using System.Diagnostics;
using GraphQL;
using NUnit.Framework;
using Speckle.Core.Api;
using Speckle.Core.Api.GraphQL.Models;
using Speckle.Core.Credentials;

namespace Speckle.Core.Tests.Unit.Api;

[TestOf(typeof(Client))]
public sealed class GraphQLClientTests : IDisposable
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

  public void Dispose()
  {
    _client?.Dispose();
  }

  private static IEnumerable<TestCaseData> ErrorCases()
  {
    yield return new TestCaseData(typeof(SpeckleGraphQLForbiddenException), new Map { { "code", "FORBIDDEN" } });
    yield return new TestCaseData(typeof(SpeckleGraphQLForbiddenException), new Map { { "code", "UNAUTHENTICATED" } });
    yield return new TestCaseData(
      typeof(SpeckleGraphQLInternalErrorException),
      new Map { { "code", "INTERNAL_SERVER_ERROR" } }
    );
    yield return new TestCaseData(typeof(SpeckleGraphQLException<FakeGqlResponseModel>), new Map { { "foo", "bar" } });
  }

  [Test, TestCaseSource(nameof(ErrorCases))]
  public void TestExceptionThrowingFromGraphQLErrors(Type exType, Map extensions)
  {
    Assert.Throws(
      exType,
      () =>
        _client.MaybeThrowFromGraphQLErrors(
          new GraphQLRequest(),
          new GraphQLResponse<FakeGqlResponseModel>
          {
            Errors = new GraphQLError[] { new() { Extensions = extensions } }
          }
        )
    );
  }

  [Test]
  public void TestMaybeThrowsDoesntThrowForNoErrors()
  {
    Assert.DoesNotThrow(() => _client.MaybeThrowFromGraphQLErrors(new GraphQLRequest(), new GraphQLResponse<string>()));
  }

  [Test]
  public void TestExecuteWithResiliencePoliciesDoesntRetryTaskCancellation()
  {
    var timer = new Stopwatch();
    timer.Start();
    Assert.ThrowsAsync<TaskCanceledException>(async () =>
    {
      var tokenSource = new CancellationTokenSource();
      tokenSource.Cancel();
      await _client.ExecuteWithResiliencePolicies(
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
    Assert.That(elapsed, Is.LessThan(1000));
  }

  [Test]
  public async Task TestExecuteWithResiliencePoliciesRetry()
  {
    var counter = 0;
    var maxRetryCount = 5;
    var expectedResult = "finally it finishes";
    var timer = new Stopwatch();
    timer.Start();
    var result = await _client.ExecuteWithResiliencePolicies(() =>
    {
      counter++;
      if (counter < maxRetryCount)
      {
        throw new SpeckleGraphQLInternalErrorException(new GraphQLRequest(), new GraphQLResponse<string>());
      }

      return Task.FromResult(expectedResult);
    });
    timer.Stop();
    // The baseline for wait is 1 seconds between the jittered retry
    Assert.That(timer.ElapsedMilliseconds, Is.GreaterThanOrEqualTo(5000));
    Assert.That(counter, Is.EqualTo(maxRetryCount));
  }

  public class FakeGqlResponseModel { }
}
