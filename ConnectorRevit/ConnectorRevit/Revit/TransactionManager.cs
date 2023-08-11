using System;
using System.Linq;
using Autodesk.Revit.DB;
using RevitSharedResources.Interfaces;
using Speckle.Core.Logging;

namespace ConnectorRevit.Revit
{
  /// <summary>
  /// implements <see cref="IRevitTransactionManager"/>
  /// </summary>
  public class TransactionManager : ITransactionManager
  {
    private string streamId;
    private Document document;
    public TransactionManager(string streamId, Document document)
    {
      this.streamId = streamId;
      this.document = document;
    }

    private ErrorEater errorEater;
    private bool isDisposed;
    private TransactionGroup transactionGroup;
    private Transaction transaction;
    private SubTransaction subTransaction;

    public void Finish()
    {
      try
      {
        Commit();
      }
      finally
      {
        if (transactionGroup.GetStatus() == TransactionStatus.Started)
        {
          transactionGroup.Assimilate();
        }
        transactionGroup?.Dispose();
      }
    }

    public void Start()
    {
      if (transactionGroup == null)
      {
        transactionGroup = new TransactionGroup(document, $"Baking stream {streamId}");
        transactionGroup.Start();
      }

      if (transaction == null
        || !transaction.IsValidObject
        || transaction.GetStatus() != TransactionStatus.Started)
      {
        transaction = new Transaction(document, $"Baking stream {streamId}");
        var failOpts = transaction.GetFailureHandlingOptions();
        errorEater = new ErrorEater();
        failOpts.SetFailuresPreprocessor(errorEater);
        failOpts.SetClearAfterRollback(true);
        transaction.SetFailureHandlingOptions(failOpts);
        transaction.Start();
      }
    }

    public TransactionStatus Commit()
    {
      if (subTransaction != null 
        && subTransaction.IsValidObject 
        && subTransaction.GetStatus() == TransactionStatus.Started)
      {
        HandleFailedCommit(subTransaction.Commit());
        subTransaction.Dispose();
      }
      if (transaction != null && transaction.IsValidObject)
      {
        var status = transaction.Commit();
        HandleFailedCommit(status);
        transaction.Dispose();
        return status;
      }
      throw new SpeckleException("Cannot commit transaction because it has not been started yet");
    }

    private void HandleFailedCommit(TransactionStatus status)
    {
      if (status == TransactionStatus.RolledBack)
      {
        var numTotalErrors = errorEater.CommitErrorsDict.Sum(kvp => kvp.Value);
        var numUniqueErrors = errorEater.CommitErrorsDict.Keys.Count;

        var exception = errorEater.GetException();
        if (exception == null)
          SpeckleLog.Logger.Fatal("Revit commit failed with {numUniqueErrors} unique errors and {numTotalErrors} total errors, but the ErrorEater did not capture any exceptions", numUniqueErrors, numTotalErrors);
        else
          SpeckleLog.Logger.Fatal(exception, "The Revit API could not resolve {numUniqueErrors} unique errors and {numTotalErrors} total errors when trying to commit the Speckle model. The whole transaction is being rolled back.", numUniqueErrors, numTotalErrors);

        throw exception ?? new SpeckleException($"The Revit API could not resolve {numUniqueErrors} unique errors and {numTotalErrors} total errors when trying to commit the Speckle model. The whole transaction is being rolled back.");
      }
    }

    public void RollbackTransaction()
    {
      RollbackSubTransaction();
      if (transaction.IsValidObject && transaction?.GetStatus() == TransactionStatus.Started)
      {
        transaction.RollBack();
      }
    }
    
    public void RollbackSubTransaction()
    {
      if (subTransaction.IsValidObject && subTransaction?.GetStatus() == TransactionStatus.Started)
      {
        subTransaction.RollBack();
      }
    }

    public void RollbackAll()
    {
      RollbackTransaction();
      if (transactionGroup.IsValidObject && transactionGroup?.GetStatus() == TransactionStatus.Started)
      {
        transactionGroup.Assimilate();
      }
    }

    public void StartSubtransaction()
    {
      Start();
      if (subTransaction == null
        || !subTransaction.IsValidObject
        || subTransaction.GetStatus() != TransactionStatus.Started)
      {
        subTransaction = new SubTransaction(document);
        subTransaction.Start();
      }
    }

    public TransactionStatus CommitSubtransaction()
    {
      if (subTransaction != null)
      {
        var status = subTransaction.Commit();
        HandleFailedCommit(status);
        subTransaction.Dispose();
        return status;
      }
      throw new SpeckleException("Cannot commit subtransaction because it has not been started yet");
    }

    #region disposal
    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
      if (isDisposed) return;

      if (disposing)
      {
        // free managed resources
        if (subTransaction?.IsValidObject is bool validSubtransaction && validSubtransaction)
          subTransaction?.Dispose();
        if (transaction?.IsValidObject is bool validTransaction && validTransaction)
          transaction?.Dispose();
        if (transaction?.IsValidObject is bool validTransactionGroup && validTransactionGroup)
          transactionGroup?.Dispose();
      }

      isDisposed = true;
    }

    ~TransactionManager()
    {
      Dispose(false);
    }
    #endregion
  }
}
