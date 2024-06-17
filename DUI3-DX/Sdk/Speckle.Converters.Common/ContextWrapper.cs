namespace Speckle.Converters.Common;

public class ContextWrapper<TDocument, THostUnit> : IDisposable
  where TDocument : class
{
  private IConversionContextStack<TDocument, THostUnit>? _stack;

  public IConversionContext<TDocument>? Context { get; private set; }

  public ContextWrapper(IConversionContextStack<TDocument, THostUnit> stack)
  {
    _stack = stack;
    Context = _stack.Current;
  }

  protected virtual void Dispose(bool disposing)
  {
    if (disposing && _stack != null)
    {
      // technically we could be popping something not this but throwing in dispose is bad
      _stack.Pop();
      _stack = null;
      Context = null;
    }
  }

  public void Dispose()
  {
    Dispose(true);
    GC.SuppressFinalize(this);
  }
}
