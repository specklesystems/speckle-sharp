using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;

namespace Speckle.Connectors.Autocad.HostApp;

public class TransactionContext : IDisposable
{
  private DocumentLock? _documentLock;
  private Transaction? _transaction;

  // Track whether Dispose has been called.
  private bool _disposed;

  // TODO: check to get rid of from static
  public static TransactionContext StartTransaction(Document document) => new(document);

  private TransactionContext(Document document)
  {
    _documentLock = document.LockDocument();
    _transaction = document.Database.TransactionManager.StartTransaction();
  }

  // Implement IDisposable.
  // Do not make this method virtual.
  // A derived class should not be able to override this method.
  public void Dispose()
  {
    Dispose(disposing: true);
    // This object will be cleaned up by the Dispose method.
    // Therefore, you should call GC.SuppressFinalize to
    // take this object off the finalization queue
    // and prevent finalization code for this object
    // from executing a second time.
    GC.SuppressFinalize(this);
  }

  // Dispose(bool disposing) executes in two distinct scenarios.
  // If disposing equals true, the method has been called directly
  // or indirectly by a user's code. Managed and unmanaged resources
  // can be disposed.
  // If disposing equals false, the method has been called by the
  // runtime from inside the finalizer and you should not reference
  // other objects. Only unmanaged resources can be disposed.
  protected virtual void Dispose(bool disposing)
  {
    // Check to see if Dispose has already been called.
    if (!_disposed)
    {
      // If disposing equals true, dispose all managed resources.
      if (disposing)
      {
        _transaction?.Commit();
        _transaction = null;

        _documentLock?.Dispose();
        _documentLock = null;
      }

      // Call the appropriate methods to clean up
      // unmanaged resources here.

      // Note disposing has been done.
      _disposed = true;
    }
  }
}
