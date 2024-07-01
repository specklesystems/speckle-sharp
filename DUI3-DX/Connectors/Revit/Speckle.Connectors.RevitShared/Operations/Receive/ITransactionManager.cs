using Autodesk.Revit.DB;

namespace Speckle.Connectors.Revit.Operations.Receive;

public interface ITransactionManager : IDisposable
{
  TransactionStatus CommitSubtransaction();
  TransactionStatus CommitTransaction();
  void CommitTransactionGroup();
  void RollbackSubTransaction();
  void RollbackTransaction();
  void RollbackTransactionGroup();
  void StartSubtransaction();
  void StartTransaction();
  void StartTransactionGroup(string transactionName);
}
