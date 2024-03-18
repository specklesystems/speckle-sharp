using Autodesk.AutoCAD.ApplicationServices;
using Document = Autodesk.AutoCAD.ApplicationServices.Document;
using Autodesk.AutoCAD.DatabaseServices;

namespace Speckle.Connectors.Autocad.HostApp;

public class TransactionContext : IDisposable
{
  private DocumentLock _documentLock;
  private Transaction _transaction;

  // TODO: check to get rid of from static
  public static TransactionContext StartTransaction(Document document) => new(document);

  private TransactionContext(Document document)
  {
    _documentLock = document.LockDocument();
    _transaction = document.Database.TransactionManager.StartTransaction();
  }

  public void Dispose()
  {
    _transaction?.Commit();
    _transaction = null;

    _documentLock?.Dispose();
    _documentLock = null;
  }
}
