using Autodesk.Revit.DB;
using Speckle.Converters.RevitShared.Helpers;

namespace Speckle.Converters.RevitShared.Services;

/// <summary>
/// Is responsible for all functionality regarding subtransactions, transactions, and transaction groups.
/// This includes starting, pausing, committing, and rolling back transactions
/// </summary>
public sealed class TransactionManagementService : ITransactionManagementService
{
  private readonly RevitConversionContextStack _contextStack;
  private Document Document => _contextStack.Current.Document.Document;

  public TransactionManagementService(RevitConversionContextStack contextStack)
  {
    _contextStack = contextStack;
  }

  private TransactionGroup? _transactionGroup;
  private Transaction? _transaction;
  private SubTransaction? _subTransaction;

  public void StartTransactionManagement(string transactionName)
  {
    if (_transactionGroup == null)
    {
      _transactionGroup = new TransactionGroup(Document, transactionName);
      _transactionGroup.Start();
    }
    StartTransaction();
  }

  public void FinishTransactionManagement()
  {
    try
    {
      CommitTransaction();
    }
    finally
    {
      if (_transactionGroup?.GetStatus() == TransactionStatus.Started)
      {
        _transactionGroup.Assimilate();
      }
      _transactionGroup?.Dispose();
    }
  }

  public void RollbackTransactionManagement()
  {
    RollbackTransaction();
    if (
      _transactionGroup != null
      && _transactionGroup.IsValidObject
      && _transactionGroup.GetStatus() == TransactionStatus.Started
    )
    {
      _transactionGroup.Assimilate();
    }
  }

  public void StartTransaction()
  {
    if (_transaction == null || !_transaction.IsValidObject || _transaction.GetStatus() != TransactionStatus.Started)
    {
      _transaction = new Transaction(Document, "Speckle Transaction");
      var failOpts = _transaction.GetFailureHandlingOptions();
      // POC: make sure to implement and add the failure preprocessor
      //failOpts.SetFailuresPreprocessor(_errorPreprocessingService);
      failOpts.SetClearAfterRollback(true);
      _transaction.SetFailureHandlingOptions(failOpts);
      _transaction.Start();
    }
  }

  public TransactionStatus CommitTransaction()
  {
    if (
      _subTransaction != null
      && _subTransaction.IsValidObject
      && _subTransaction.GetStatus() == TransactionStatus.Started
    )
    {
      var status = _subTransaction.Commit();
      if (status != TransactionStatus.Committed)
      {
        // POC: handle failed commit
        //HandleFailedCommit(status);
      }
      _subTransaction.Dispose();
    }
    if (_transaction != null && _transaction.IsValidObject && _transaction.GetStatus() == TransactionStatus.Started)
    {
      var status = _transaction.Commit();
      if (status != TransactionStatus.Committed)
      {
        // POC: handle failed commit
        //HandleFailedCommit(status);
      }
      _transaction.Dispose();
      return status;
    }
    return TransactionStatus.Uninitialized;
  }

  public void RollbackTransaction()
  {
    RollbackSubTransaction();
    if (_transaction != null && _transaction.IsValidObject && _transaction.GetStatus() == TransactionStatus.Started)
    {
      _transaction.RollBack();
    }
  }

  public void StartSubtransaction()
  {
    StartTransaction();
    if (
      _subTransaction == null
      || !_subTransaction.IsValidObject
      || _subTransaction.GetStatus() != TransactionStatus.Started
    )
    {
      _subTransaction = new SubTransaction(Document);
      _subTransaction.Start();
    }
  }

  public TransactionStatus CommitSubtransaction()
  {
    if (_subTransaction != null && _subTransaction.IsValidObject)
    {
      var status = _subTransaction.Commit();
      if (status != TransactionStatus.Committed)
      {
        // POC: handle failed commit
        //HandleFailedCommit(status);
      }
      _subTransaction.Dispose();
      return status;
    }
    return TransactionStatus.Uninitialized;
  }

  public void RollbackSubTransaction()
  {
    if (
      _subTransaction != null
      && _subTransaction.IsValidObject
      && _subTransaction.GetStatus() == TransactionStatus.Started
    )
    {
      _subTransaction.RollBack();
    }
  }

  public TResult ExecuteInTemporaryTransaction<TResult>(Func<TResult> function)
  {
    return ExecuteInTemporaryTransaction(function, Document);
  }

  public static TResult ExecuteInTemporaryTransaction<TResult>(Func<TResult> function, Document document)
  {
    TResult result = default;
    if (!document.IsModifiable)
    {
      using var t = new Transaction(document, "This Transaction Will Never Get Committed");
      try
      {
        t.Start();
        result = function();
      }
      catch (Autodesk.Revit.Exceptions.ApplicationException ex)
      {
        // ignore because we're just going to rollback
        // POC: logging
      }
      finally
      {
        t.RollBack();
      }
    }
    else
    {
      using var t = new SubTransaction(document);
      try
      {
        t.Start();
        result = function();
      }
      catch (Autodesk.Revit.Exceptions.ApplicationException ex)
      {
        // ignore because we're just going to rollback
        // POC: logging
      }
      finally
      {
        t.RollBack();
      }
    }

    return result;
  }

  public void Dispose()
  {
    // free managed resources
    if (_subTransaction != null && _subTransaction.IsValidObject)
    {
      _subTransaction.Dispose();
    }

    if (_transaction != null && _transaction.IsValidObject)
    {
      _transaction.Dispose();
    }

    if (_transactionGroup != null && _transactionGroup.IsValidObject)
    {
      _transactionGroup.Dispose();
    }
  }
}
