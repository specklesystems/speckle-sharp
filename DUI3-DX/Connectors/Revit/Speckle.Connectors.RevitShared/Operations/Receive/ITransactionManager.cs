using Autodesk.Revit.DB;

namespace Speckle.Connectors.Revit.Operations.Receive;

public interface ITransactionManager : IDisposable
{
  TransactionStatus CommitSubtransaction();
  TransactionStatus CommitTransaction();
  void RollbackSubTransaction();
  void RollbackTransaction();
  void StartSubtransaction();
  void StartTransaction();
}
