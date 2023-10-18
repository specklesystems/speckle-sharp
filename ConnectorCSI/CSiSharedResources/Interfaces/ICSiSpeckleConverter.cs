using Speckle.Core.Kits;

namespace CSiSharedResources.Interfaces
{
  public interface ICSiSpeckleConverter : ISpeckleConverter
  {
    void CommitAllDatabaseTableChanges();
  }
}
