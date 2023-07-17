using System;
using System.Linq;
using Autodesk.Revit.DB;
using ConnectorRevit.Revit;
using RevitSharedResources.Interfaces;
using Speckle.Core.Logging;

namespace ConnectorRevit.Services
{
  internal class TransactionManager : IRevitTransactionManager
  {
    private ErrorEater errorEater;
    public TransactionManager(ErrorEater errorEater)
    {
      this.errorEater = errorEater;
    }

    private TransactionGroup transactionGroup;
    private Transaction transaction;

    public void Finish()
    {
      transaction.Commit();
      transactionGroup.Assimilate();
    }

    public void Start(string streamId, Document document)
    {
      string transactionName = $"Baking stream {streamId}";
      transactionGroup = new TransactionGroup(document, transactionName);
      transaction = new Transaction(document, transactionName);

      transactionGroup.Start();
      var failOpts = transaction.GetFailureHandlingOptions();
      failOpts.SetFailuresPreprocessor(errorEater);
      failOpts.SetClearAfterRollback(true);
      transaction.SetFailureHandlingOptions(failOpts);
      transaction.Start();
    }

    public void Dispose()
    {
      transaction.Dispose();
      transactionGroup.Dispose();
    }

    public TransactionStatus Commit()
    {
      var status = transaction.Commit();

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

      return status;
    }

    public void Resume()
    {
      transaction.Start();
    }

    public void Rollback()
    {
      transaction.RollBack();
      transactionGroup.RollBack();
    }
  }
}
