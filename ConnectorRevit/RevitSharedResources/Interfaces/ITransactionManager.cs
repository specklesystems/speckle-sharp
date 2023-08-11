using System;
using Autodesk.Revit.DB;

namespace RevitSharedResources.Interfaces
{
  /// <summary>
  /// Is responsible for starting, pausing, committing, and rolling back a Revit transaction
  /// </summary>
  public interface ITransactionManager : IDisposable
  {
    void Start();
    void StartSubtransaction();
    TransactionStatus Commit();
    TransactionStatus CommitSubtransaction();
    void Finish();
    void RollbackTransaction();
    void RollbackSubTransaction();
    void RollbackAll();
  }
}
