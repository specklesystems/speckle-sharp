using System;
using System.Collections.Generic;
using ConverterCSIShared.Services;
using CSiAPIv1;

namespace ConverterCSIShared.Models
{
  internal abstract class DatabaseTableWrapper
  {
    public abstract string TableKey { get; }
    protected readonly cSapModel cSapModel;
    protected readonly ToNativeScalingService toNativeScalingService;
    private int tableVersion;
    protected string[] fieldKeysIncluded;
    protected int numRecords;
    private readonly List<string> tableData;

    protected DatabaseTableWrapper(cSapModel cSapModel, ToNativeScalingService toNativeScalingService)
    {
      this.cSapModel = cSapModel;
      this.toNativeScalingService = toNativeScalingService;
      this.tableData = new List<string>(GetTableData());
    }
    private string[] GetTableData()
    {
      var tableData = Array.Empty<string>();
      cSapModel.DatabaseTables.GetTableForEditingArray(
        TableKey,
        "this param does nothing",
        ref tableVersion,
        ref fieldKeysIncluded,
        ref numRecords,
        ref tableData
      );
      return tableData;
    }

    protected void AddRow(params string[] arguments)
    {
      if (arguments.Length != fieldKeysIncluded.Length)
      {
        throw new ArgumentException($"Method {nameof(AddRow)} was passed an array of length {arguments.Length}, but was expecting an array of length {fieldKeysIncluded.Length}");
      }
      tableData.AddRange(arguments);
      numRecords++;
    }

    public void ApplyEditedTables()
    {
      var tableDataArray = tableData.ToArray();
      cSapModel.DatabaseTables.SetTableForEditingArray(TableKey, ref tableVersion, ref fieldKeysIncluded, numRecords, ref tableDataArray);

      int numFatalErrors = 0;
      int numWarnMsgs = 0;
      int numInfoMsgs = 0;
      int numErrorMsgs = 0;
      string importLog = "";
      cSapModel.DatabaseTables.ApplyEditedTables(false, ref numFatalErrors, ref numErrorMsgs, ref numWarnMsgs, ref numInfoMsgs, ref importLog);
    }
  }
}
