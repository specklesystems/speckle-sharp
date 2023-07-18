using System;
using System.Collections.Generic;
using System.Text;
using Autodesk.Revit.DB;

namespace RevitSharedResources.Interfaces
{
  /// <summary>
  /// Is responsible for starting, pausing, committing, and rolling back a Revit transaction
  /// </summary>
  public interface IRevitTransactionManager : IDisposable
  {
    void Start(string streamId, Document document);
    TransactionStatus Commit();
    void Resume();
    void Finish();
    void Rollback();
  }
}
