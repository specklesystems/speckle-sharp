using ConverterCSIShared.Models;

namespace Objects.Converter.CSI;

public partial class ConverterCSI
{
  private ETABSGridLineDefinitionTable gridLineDefinitionTable;
  private ETABSGridLineDefinitionTable GridLineDefinitionTable => gridLineDefinitionTable ??= new(Model, new(Model));

  public void CommitAllDatabaseTableChanges()
  {
    GridLineDefinitionTable.CommitPendingChanges();
  }
}
