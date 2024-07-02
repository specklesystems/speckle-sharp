using Autodesk.Revit.DB;
using Speckle.Converters.RevitShared.Helpers;

namespace Speckle.Connectors.Revit.Operations.Receive;

/// <summary>
/// Is responsible for all functionality regarding subtransactions, transactions, and transaction groups.
/// This includes starting, pausing, committing, and rolling back transactions
/// </summary>
public sealed class TransactionManager : ITransactionManager
{
  private readonly IRevitConversionContextStack _contextStack;
  private Document Document => _contextStack.Current.Document;

  public TransactionManager(IRevitConversionContextStack contextStack)
  {
    _contextStack = contextStack;
  }

  // poc : these are being disposed. I'm not sure why I need to supress this warning
#pragma warning disable CA2213 // Disposable fields should be disposed
  private Transaction? _transaction;
  private SubTransaction? _subTransaction;
#pragma warning restore CA2213 // Disposable fields should be disposed

  public void StartTransaction()
  {
    if (_transaction == null || !_transaction.IsValidObject || _transaction.GetStatus() != TransactionStatus.Started)
    {
      _transaction = new Transaction(Document, "Speckle Transaction");
      var failOpts = _transaction.GetFailureHandlingOptions();
      // POC: make sure to implement and add the failure preprocessor
      // https://spockle.atlassian.net/browse/DUI3-461
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
    }
    if (_transaction != null && _transaction.IsValidObject && _transaction.GetStatus() == TransactionStatus.Started)
    {
      var status = _transaction.Commit();
      if (status != TransactionStatus.Committed)
      {
        // POC: handle failed commit
        //HandleFailedCommit(status);
      }
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

  public void Dispose()
  {
    _subTransaction?.Dispose();
    _transaction?.Dispose();
  }
}
