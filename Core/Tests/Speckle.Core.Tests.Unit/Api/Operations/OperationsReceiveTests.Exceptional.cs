using NUnit.Framework;
using Speckle.Core.Transports;

namespace Speckle.Core.Tests.Unit.Api.Operations;

public partial class OperationsReceiveTests
{
  [Test, TestCaseSource(nameof(TestCases))]
  public void Receive_ObjectsDontExist_ExceptionThrown(string id)
  {
    MemoryTransport emptyTransport1 = new();
    MemoryTransport emptyTransport2 = new();
    Assert.ThrowsAsync<TransportException>(async () =>
    {
      await Core.Api.Operations.Receive(id, emptyTransport1, emptyTransport2);
    });
  }

  [Test, TestCaseSource(nameof(TestCases))]
  public void Receive_ObjectsDontExistNullRemote_ExceptionThrown(string id)
  {
    MemoryTransport emptyTransport = new();
    Assert.ThrowsAsync<TransportException>(async () =>
    {
      await Core.Api.Operations.Receive(id, null, emptyTransport);
    });
  }

  [Test, TestCaseSource(nameof(TestCases))]
  public void Receive_OperationCanceled_ExceptionThrown(string id)
  {
    using CancellationTokenSource ctc = new();
    ctc.Cancel();

    MemoryTransport emptyTransport2 = new();
    Assert.CatchAsync<OperationCanceledException>(async () =>
    {
      await Core.Api.Operations.Receive(id, _testCaseTransport, emptyTransport2, cancellationToken: ctc.Token);
    });
  }
}
