using System;
using System.Collections.Generic;
using System.Text;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;

namespace Speckle.ConnectorAutocadCivil.DocumentUtils
{
  public class TransactionContext : IDisposable
  {
    private DocumentLock DocumentLock;
    private Transaction Transaction;

    public static TransactionContext StartTransaction(Document document)
    {
      TransactionContext transactionContext = new TransactionContext(document);

      return transactionContext;
    }

    private TransactionContext(Document document)
    {
      DocumentLock = document.LockDocument();
      Transaction = document.Database.TransactionManager.StartTransaction();
    }

    public void Dispose()
    {
      Transaction.Commit();
      DocumentLock?.Dispose();
    }
  }
}
