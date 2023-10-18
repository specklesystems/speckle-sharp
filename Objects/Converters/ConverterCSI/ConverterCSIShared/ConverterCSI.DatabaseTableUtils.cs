using ConverterCSIShared.Models;
using CSiConnectorConverterShared.Interfaces;

namespace Objects.Converter.CSI
{
  public partial class ConverterCSI : ICSiSpeckleConverter
  {
    private ETABSGridLineDefinitionTable gridLineDefinitionTable;
    private ETABSGridLineDefinitionTable GridLineDefinitionTable => gridLineDefinitionTable ??= new(Model, new(Model));
    public void CommitAllDatabaseTableChanges()
    {
      GridLineDefinitionTable.CommitPendingChanges();
    }
  }
}
