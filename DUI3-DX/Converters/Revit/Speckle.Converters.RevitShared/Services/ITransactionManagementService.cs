using Autodesk.Revit.DB;

namespace Speckle.Converters.RevitShared.Services;

public interface ITransactionManagementService : IDisposable
{
  public void StartTransactionManagement(string transactionName);
  public void FinishTransactionManagement();
  public void RollbackTransactionManagement();

  public void StartTransaction();
  public TransactionStatus CommitTransaction();
  public void RollbackTransaction();

  public void StartSubtransaction();
  public TransactionStatus CommitSubtransaction();
  public void RollbackSubTransaction();
}
