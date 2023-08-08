using System;
using Autodesk.AutoCAD.ApplicationServices;
using Document = Autodesk.AutoCAD.ApplicationServices.Document;
using Autodesk.AutoCAD.DatabaseServices;

namespace AutocadCivilDUI3Shared.Utils
{
  public class TransactionContext : IDisposable
  {
    private DocumentLock DocumentLock;
    private Transaction Transaction;

    public static TransactionContext StartTransaction(Document document)
    {
      return new TransactionContext(document);
    }

    private TransactionContext(Document document)
    {
      DocumentLock = document.LockDocument();
      Transaction = document.Database.TransactionManager.StartTransaction();
    }

    public void Dispose()
    {
      Transaction?.Commit();
      Transaction = null;

      DocumentLock?.Dispose();
      DocumentLock = null;
    }
  }
}
