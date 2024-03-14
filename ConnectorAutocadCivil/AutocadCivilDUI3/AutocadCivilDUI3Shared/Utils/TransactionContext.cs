using System;
using Autodesk.AutoCAD.ApplicationServices;
using Document = Autodesk.AutoCAD.ApplicationServices.Document;
using Autodesk.AutoCAD.DatabaseServices;

namespace AutocadCivilDUI3Shared.Utils;

public class TransactionContext : IDisposable
{
  private DocumentLock _documentLock;
  private Transaction _transaction;

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
