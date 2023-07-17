using System;
using System.Collections.Generic;
using System.Text;
using Autodesk.Revit.DB;

namespace RevitSharedResources.Interfaces
{
  public interface IRevitTransactionManager : IDisposable
  {
    void Start(string streamId, Document document);
    TransactionStatus Commit();
    void Resume();
    void Finish();
    void Rollback();
  }
}
