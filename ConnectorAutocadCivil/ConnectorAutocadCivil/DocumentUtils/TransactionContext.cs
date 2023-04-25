using System;
using System.Collections.Generic;
using System.Text;
using Autodesk.AutoCAD.ApplicationServices;
using Document = Autodesk.AutoCAD.ApplicationServices.Document;

#if ADVANCESTEEL2023
using Autodesk.AdvanceSteel.DocumentManagement;
#else
using Autodesk.AutoCAD.DatabaseServices;
#endif

namespace Speckle.ConnectorAutocadCivil.DocumentUtils
{
#if ADVANCESTEEL2023
  public class TransactionContext : IDisposable
  {
    private bool DocumentLocked = false;
    private Autodesk.AdvanceSteel.CADAccess.Transaction Transaction = null;

    public static TransactionContext StartTransaction(Document document)
    {
      return new TransactionContext(document);
    }

    private TransactionContext(Document document)
    {
      if (!DocumentLocked)
      {
        DocumentLocked = DocumentManager.LockCurrentDocument();
      }

      if (Transaction == null && DocumentLocked)
      {
        Transaction = Autodesk.AdvanceSteel.CADAccess.TransactionManager.StartTransaction();
      }
    }

    public void Dispose()
    {
      Transaction?.Commit();
      Transaction = null;

      if (DocumentLocked == true)
      {
        DocumentManager.UnlockCurrentDocument();
        DocumentLocked = false;
      }
    }
  }
#else
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
#endif
}
