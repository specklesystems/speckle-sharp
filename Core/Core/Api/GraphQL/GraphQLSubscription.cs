using System;
using GraphQL;

namespace Speckle.Core.Api.GraphQL;

public sealed class GraphQLSubscription<T> : IDisposable
{
  private readonly ISpeckleGraphQLSubscriber _client;
  private readonly GraphQLRequest _request;
  private IDisposable? _socket;
  private volatile bool _isDisposed;

  internal GraphQLSubscription(ISpeckleGraphQLSubscriber client, GraphQLRequest request)
  {
    _client = client;
    _request = request;
  }

  private event Action<object, T> _callback;
  public event Action<object, T> Callback
  {
    add
    {
      if (_isDisposed)
      {
        throw new ObjectDisposedException("Client resource is disposed");
      }
      _socket ??= StartSubscription();
      _callback += value;
    }
    remove
    {
      _callback -= value;
      if (HasSubscribers)
      {
        _socket?.Dispose();
        _socket = null;
      }
    }
  }

  private bool HasSubscribers => _callback is null || _callback.GetInvocationList().Length != 0;

  private IDisposable StartSubscription()
  {
    var respose = _client.SubscribeTo<T>(
      _request,
      (o, v) =>
      {
        _callback.Invoke(o, v);
      }
    );
    return respose;
  }

  public void Dispose()
  {
    _socket?.Dispose();
    _isDisposed = true;
  }
}
