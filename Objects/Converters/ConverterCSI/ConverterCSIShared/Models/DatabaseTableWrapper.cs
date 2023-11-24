using System;
using System.Collections.Generic;
using ConverterCSIShared.Services;
using CSiAPIv1;
using Speckle.Core.Logging;

namespace ConverterCSIShared.Models;

internal abstract class DatabaseTableWrapper
{
  public abstract string TableKey { get; }

  protected readonly cSapModel cSapModel;
  protected readonly ToNativeScalingService toNativeScalingService;

  private int tableVersion;
  protected string[] fieldKeysIncluded;
  private int numRecords;
  private List<string> tableData;

  private readonly List<string[]> rowsToAdd = new();

  protected DatabaseTableWrapper(cSapModel cSapModel, ToNativeScalingService toNativeScalingService)
  {
    this.cSapModel = cSapModel;
    this.toNativeScalingService = toNativeScalingService;
    RefreshTableData();
  }

  private void RefreshTableData()
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
    this.tableData = new List<string>(tableData);
  }

  protected void AddRowToBeCommitted(params string[] arguments)
  {
    if (arguments.Length != fieldKeysIncluded.Length)
    {
      throw new ArgumentException(
        $"Method {nameof(AddRowToBeCommitted)} was passed an array of length {arguments.Length}, but was expecting an array of length {fieldKeysIncluded.Length}"
      );
    }
    rowsToAdd.Add(arguments);
  }

  public void CommitPendingChanges()
  {
    foreach (string[] row in rowsToAdd)
    {
      tableData.AddRange(row);
      numRecords++;
    }
    ApplyTablesEditedTables();
  }

  private void ApplyTablesEditedTables()
  {
    var tableDataArray = tableData.ToArray();
    cSapModel.DatabaseTables.SetTableForEditingArray(
      TableKey,
      ref tableVersion,
      ref fieldKeysIncluded,
      numRecords,
      ref tableDataArray
    );

    int numFatalErrors = 0;
    int numWarnMsgs = 0;
    int numInfoMsgs = 0;
    int numErrorMsgs = 0;
    string importLog = "";
    cSapModel.DatabaseTables.ApplyEditedTables(
      false,
      ref numFatalErrors,
      ref numErrorMsgs,
      ref numWarnMsgs,
      ref numInfoMsgs,
      ref importLog
    );

    if (numFatalErrors == 0 && numErrorMsgs == 0)
    {
      SpeckleLog.Logger.Error(
        "{numErrors} errors and {numFatalErrors} fatal errors occurred when attempting to add {numRowsToAdd} rows to table with key, {tableKey}",
        numErrorMsgs,
        numFatalErrors,
        rowsToAdd.Count,
        TableKey
      );
    }
    rowsToAdd.Clear();
  }
}
