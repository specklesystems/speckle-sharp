using Speckle.Core.Kits;

namespace CSiConnectorConverterShared.Interfaces
{
  public interface ICSiSpeckleConverter : ISpeckleConverter
  {
    void CommitAllDatabaseTableChanges();
  }
}
