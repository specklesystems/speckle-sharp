using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Objects.Organization;
using RevitSharedResources.Models;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using DB = Autodesk.Revit.DB;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    #region ToNative
    private ApplicationObject DataTableToNative(DataTable speckleTable)
    {
      var docObj = GetExistingElementByApplicationId(speckleTable.applicationId);
      var appObj = new ApplicationObject(speckleTable.id, speckleTable.speckle_type)
      {
        applicationId = speckleTable.applicationId
      };

      if (docObj == null)
      {
        throw new NotSupportedException("Creating brand new schedules is currently not supported");
      }

      if (docObj is not ViewSchedule revitSchedule)
      {
        throw new Exception(
          $"Existing element with UniqueId = {docObj.UniqueId} is of the type {docObj.GetType()}, not of the expected type, DB.ViewSchedule"
        );
      }

      var speckleIndexToRevitParameterDataMap = new Dictionary<int, RevitParameterData>();
      foreach (var columnInfo in RevitScheduleUtils.ScheduleColumnIteration(revitSchedule))
      {
        AddToIndexToScheduleMap(columnInfo, speckleTable, speckleIndexToRevitParameterDataMap);
      }

      var originalTableIds = new FilteredElementCollector(Doc, revitSchedule.Id).ToElementIds();
      foreach (var rowInfo in RevitScheduleUtils.ScheduleRowIteration(revitSchedule))
      {
        UpdateDataInRow(rowInfo, originalTableIds, revitSchedule, speckleTable, speckleIndexToRevitParameterDataMap);
      }

      appObj.Update(convertedItem: docObj, createdId: docObj.UniqueId, status: ApplicationObject.State.Updated);
      return appObj;
    }

    private static void AddToIndexToScheduleMap(
      ScheduleColumnIterationInfo info,
      DataTable speckleTable,
      Dictionary<int, RevitParameterData> speckleIndexToRevitParameterDataMap
    )
    {
      var fieldInt = info.field.ParameterId.IntegerValue;

      var incomingColumnIndex = -1;
      for (var i = 0; i < speckleTable.columnMetadata.Count; i++)
      {
        long? paramId = null;
        if (speckleTable.columnMetadata[i]["BuiltInParameterInteger"] is int paramIdInt)
        {
          paramId = paramIdInt;
        }
        if (speckleTable.columnMetadata[i]["BuiltInParameterInteger"] is long paramIdLong)
        {
          paramId = paramIdLong;
        }

        if (paramId == null)
        {
          continue;
        }
        if (paramId != fieldInt)
        {
          continue;
        }

        incomingColumnIndex = i;
        break;
      }

      if (incomingColumnIndex == -1)
      {
        return;
      }

      if (!(speckleTable.columnMetadata[incomingColumnIndex]["FieldType"] is string fieldType))
      {
        throw new Exception("Column does not have prop metadata. FieldType is missing");
      }

      var scheduleData = new RevitParameterData
      {
        ColumnIndex = info.columnIndex - info.numHiddenFields,
        Parameter = (BuiltInParameter)fieldInt,
        IsTypeParam = fieldType == "ElementType"
      };
      speckleIndexToRevitParameterDataMap.Add(incomingColumnIndex, scheduleData);
    }

    private void UpdateDataInRow(
      ScheduleRowIterationInfo info,
      ICollection<ElementId> originalTableIds,
      ViewSchedule revitSchedule,
      DataTable speckleTable,
      Dictionary<int, RevitParameterData> speckleIndexToRevitParameterDataMap
    )
    {
      var elementIds = ElementApplicationIdsInRow(
          info.rowIndex,
          info.section,
          originalTableIds,
          revitSchedule,
          info.tableSection
        )
        .ToList();

      if (elementIds.Count == 0)
      {
        return;
      }

      var speckleObjectRowIndex = speckleTable.rowMetadata.FindIndex(
        b => b["RevitApplicationIds"] is IList list && list.Contains(elementIds.First())
      );

      foreach (var kvp in speckleIndexToRevitParameterDataMap)
      {
        var speckleObjectColumnIndex = kvp.Key;
        var revitScheduleData = kvp.Value;

        var existingValue = revitSchedule.GetCellText(info.tableSection, info.rowIndex, revitScheduleData.ColumnIndex);
        var newValue = speckleTable.data[speckleObjectRowIndex][speckleObjectColumnIndex];
        if (existingValue == newValue.ToString())
        {
          continue;
        }

        if (revitScheduleData.IsTypeParam)
        {
          Element element = null;
          foreach (var id in elementIds)
          {
            element = Doc.GetElement(elementIds.First());
            if (element != null)
            {
              break;
            }
          }
          if (element == null)
          {
            return;
          }

          var elementType = Doc.GetElement(element.GetTypeId());
          TrySetParam(elementType, revitScheduleData.Parameter, newValue, "none");
        }
        else
        {
          foreach (var id in elementIds)
          {
            var element = Doc.GetElement(id);
            if (element == null)
              continue;

            TrySetParam(element, revitScheduleData.Parameter, newValue, "none");
          }
        }
      }
    }

    #endregion

    #region ToSpeckle
    private DataTable ScheduleToSpeckle(DB.ViewSchedule revitSchedule)
    {
      var speckleTable = new DataTable { applicationId = revitSchedule.UniqueId };

      var originalTableIds = new FilteredElementCollector(Doc, revitSchedule.Id).ToElementIds();

      var skippedIndicies = new Dictionary<SectionType, List<int>>();
      var columnHeaders = new List<string>();

      DefineColumnMetadata(revitSchedule, speckleTable, originalTableIds, columnHeaders);

      var headerIndexArray = GetTableHeaderIndexArray(revitSchedule, columnHeaders);

      PopulateDataTableRows(revitSchedule, speckleTable, originalTableIds, headerIndexArray, columnHeaders);

      if (!revitSchedule.Definition.ShowHeaders)
      {
        AddHeaderRow(speckleTable, columnHeaders);
      }

      return speckleTable;
    }

    private void AddHeaderRow(DataTable speckleTable, List<string> headers)
    {
      speckleTable.AddRow(metadata: new Base(), index: speckleTable.headerRowIndex, headers.ToArray());
    }

    private void DefineColumnMetadata(
      ViewSchedule revitSchedule,
      DataTable speckleTable,
      ICollection<ElementId> originalTableIds,
      List<string> columnHeaders
    )
    {
      Element firstElement = null;
      Element firstType = null;
      if (originalTableIds.Count > 0)
      {
        firstElement = Doc.GetElement(originalTableIds.First());
        firstType = Doc.GetElement(firstElement.GetTypeId());
      }

      foreach (var columnInfo in RevitScheduleUtils.ScheduleColumnIteration(revitSchedule))
      {
        AddColumnMetadataToDataTable(columnInfo, revitSchedule, speckleTable, columnHeaders, firstType, firstElement);
      }
    }

    private static void AddColumnMetadataToDataTable(
      ScheduleColumnIterationInfo info,
      ViewSchedule revitSchedule,
      DataTable speckleTable,
      List<string> columnHeaders,
      Element firstType,
      Element firstElement
    )
    {
      // add column header to list for potential future use
      columnHeaders.Add(info.field.ColumnHeading);

      var builtInParameter = (BuiltInParameter)info.field.ParameterId.IntegerValue;

      var columnMetadata = new Base();
      columnMetadata["BuiltInParameterInteger"] = info.field.ParameterId.IntegerValue;
      columnMetadata["FieldType"] = info.field.FieldType.ToString();

      Parameter param;
      if (info.field.FieldType == ScheduleFieldType.ElementType)
      {
        if (firstType != null)
        {
          param = firstType.get_Parameter(builtInParameter);
          columnMetadata["IsReadOnly"] = param?.IsReadOnly;
        }
      }
      else if (info.field.FieldType == ScheduleFieldType.Instance)
      {
        if (firstElement != null)
        {
          param = firstElement.get_Parameter(builtInParameter);
          columnMetadata["IsReadOnly"] = param?.IsReadOnly;
        }
      }
      else
      {
        var scheduleCategory = (BuiltInCategory)revitSchedule.Definition.CategoryId.IntegerValue;
        SpeckleLog.Logger.Warning(
          "Schedule of category, {scheduleCategory}, contains field of type {builtInParameter} which has an unsupported field type, {fieldType}",
          scheduleCategory,
          builtInParameter,
          info.field.FieldType.ToString()
        );
      }
      speckleTable.DefineColumn(columnMetadata);
    }

    private void PopulateDataTableRows(
      ViewSchedule revitSchedule,
      DataTable speckleTable,
      ICollection<ElementId> originalTableIds,
      int[] columnHeaderIndexArray,
      List<string> columnHeaders
    )
    {
      var minHeaderIndex = columnHeaderIndexArray.Min();
      var maxHeaderIndex = columnHeaderIndexArray.Max();
      speckleTable.headerRowIndex = maxHeaderIndex;
      foreach (var rowInfo in RevitScheduleUtils.ScheduleRowIteration(revitSchedule))
      {
        var rowValues = new List<string>();

        if (rowInfo.masterRowIndex == maxHeaderIndex)
        {
          rowValues = columnHeaders;
        }
        else
        {
          rowValues = GetRowValues(revitSchedule, rowInfo.tableSection, rowInfo.columnCount, rowInfo.rowIndex);
        }

        if (rowInfo.masterRowIndex >= minHeaderIndex && rowInfo.masterRowIndex < maxHeaderIndex)
        {
          for (var i = 0; i < rowInfo.columnCount; i++)
          {
            if (columnHeaderIndexArray[i] == rowInfo.masterRowIndex)
            {
              rowValues[i] = string.Empty;
            }
          }
        }

        var rowAdded = AddRowToSpeckleTable(
          revitSchedule,
          speckleTable,
          originalTableIds,
          rowInfo.tableSection,
          rowInfo.section,
          rowValues,
          rowInfo.rowIndex
        );
        if (!rowAdded && rowInfo.masterRowIndex < maxHeaderIndex)
        {
          speckleTable.headerRowIndex--;
        }
      }
    }

    private int[] GetTableHeaderIndexArray(ViewSchedule revitSchedule, List<string> columnHeaders)
    {
      if (!revitSchedule.Definition.ShowHeaders)
      {
        return TransactionManager.ExecuteInTemporaryTransaction(
          () =>
          {
            revitSchedule.Definition.ShowHeaders = true;
            return GetHeaderIndexArrayFromScheduleWithHeaders(revitSchedule, columnHeaders);
          },
          revitSchedule.Document
        );
      }

      return GetHeaderIndexArrayFromScheduleWithHeaders(revitSchedule, columnHeaders);
    }

    private static int[] GetHeaderIndexArrayFromScheduleWithHeaders(
      ViewSchedule revitSchedule,
      List<string> columnHeaders
    )
    {
      string nextCellValue = null;
      var headerIndexArray = new int[columnHeaders.Count];
      var headersSetArray = new bool[columnHeaders.Count];
      foreach (var rowInfo in RevitScheduleUtils.ScheduleRowIteration(revitSchedule))
      {
        if (rowInfo.columnCount != columnHeaders.Count)
        {
          continue;
        }

        for (var columnIndex = 0; columnIndex < rowInfo.columnCount; columnIndex++)
        {
          if (headersSetArray[columnIndex])
          {
            continue;
          }

          var cellValue = revitSchedule.GetCellText(rowInfo.tableSection, rowInfo.rowIndex, columnIndex);
          if (cellValue != columnHeaders[columnIndex])
          {
            continue;
          }

          headerIndexArray[columnIndex] = rowInfo.masterRowIndex;
          headersSetArray[columnIndex] = true;

          if (!headersSetArray.Where(o => o == false).Any())
          {
            return headerIndexArray;
          }
        }
      }
      return Enumerable.Repeat(0, columnHeaders.Count).ToArray();
    }

    private bool AddRowToSpeckleTable(
      ViewSchedule revitSchedule,
      DataTable speckleTable,
      ICollection<ElementId> originalTableIds,
      SectionType tableSection,
      TableSectionData section,
      List<string> rowValues,
      int rowIndex
    )
    {
      if (!rowValues.Where(s => !string.IsNullOrEmpty(s)).Any())
      {
        return false;
      }
      var metadata = new Base();
      metadata["RevitApplicationIds"] = ElementApplicationIdsInRow(
          rowIndex,
          section,
          originalTableIds,
          revitSchedule,
          tableSection
        )
        .ToList();

      try
      {
        speckleTable.AddRow(metadata: metadata, objects: rowValues.ToArray());
      }
      catch (ArgumentException)
      {
        // trying to add an invalid row. Just don't add it and continue to the next
        return false;
      }

      return true;
    }

    private static List<string> GetRowValues(
      ViewSchedule revitSchedule,
      SectionType tableSection,
      int columnCount,
      int rowIndex
    )
    {
      var rowData = new List<string>();
      for (var columnIndex = 0; columnIndex < columnCount; columnIndex++)
      {
        rowData.Add(revitSchedule.GetCellText(tableSection, rowIndex, columnIndex));
      }

      return rowData;
    }
    #endregion

    public static IEnumerable<string> ElementApplicationIdsInRow(
      int rowNumber,
      TableSectionData section,
      ICollection<ElementId> orginialTableIds,
      DB.ViewSchedule revitSchedule,
      SectionType tableSection
    )
    {
      var remainingIdsInRow = TransactionManager.ExecuteInTemporaryTransaction(
        () =>
        {
          section.RemoveRow(rowNumber);
          return new FilteredElementCollector(revitSchedule.Document, revitSchedule.Id).ToElementIds().ToList();
        },
        revitSchedule.Document
      );

      // the section must be recomputed here because of our hacky row deleting trick
      var table = revitSchedule.GetTableData();
      section = table.GetSectionData(tableSection);

      if (remainingIdsInRow == null || remainingIdsInRow.Count == orginialTableIds.Count)
      {
        yield break;
      }

      foreach (var id in orginialTableIds)
      {
        if (remainingIdsInRow.Contains(id))
          continue;
        yield return revitSchedule.Document.GetElement(id).UniqueId;
      }
    }
  }

  public struct RevitParameterData
  {
    public int ColumnIndex;
    public BuiltInParameter Parameter;
    public bool IsTypeParam;
  }

  public struct ScheduleRowIterationInfo
  {
    public SectionType tableSection;
    public TableSectionData section;
    public int rowIndex;
    public int columnCount;
    public int masterRowIndex;
  }

  public struct ScheduleColumnIterationInfo
  {
    public ScheduleField field;
    public int columnIndex;
    public int columnCount;
    public int numHiddenFields;
  }

  public static class RevitScheduleUtils
  {
    public static IEnumerable<ScheduleRowIterationInfo> ScheduleRowIteration(
      ViewSchedule revitSchedule,
      Dictionary<SectionType, List<int>> skippedIndicies = null
    )
    {
      var masterRowIndex = 0;

      foreach (SectionType tableSection in Enum.GetValues(typeof(SectionType)))
      {
        // the table must be recomputed here because of our hacky row deleting trick
        var table = revitSchedule.GetTableData();
        var section = table.GetSectionData(tableSection);

        if (section == null)
        {
          continue;
        }
        var rowCount = section.NumberOfRows;
        var columnCount = section.NumberOfColumns;

        for (var rowIndex = 0; rowIndex < rowCount; rowIndex++)
        {
          yield return new ScheduleRowIterationInfo
          {
            tableSection = tableSection,
            section = section,
            rowIndex = rowIndex,
            columnCount = columnCount,
            masterRowIndex = masterRowIndex
          };

          if (
            skippedIndicies == null
            || !skippedIndicies.TryGetValue(tableSection, out var indicies)
            || !indicies.Contains(rowIndex)
          )
          {
            // this "skippedIndicies" dict contains the indicies that contain only empty values
            // these values were skipped when adding them to the DataTable, so the indicies of the revitSchedule
            // and the Speckle DataTable will differ at these indicies (and all subsequent indicies)

            // therefore we only want to increment the masterRowIndex if this row was added to the Speckle DataTable
            masterRowIndex++;
          }
        }
      }
    }

    public static IEnumerable<ScheduleColumnIterationInfo> ScheduleColumnIteration(ViewSchedule revitSchedule)
    {
      var scheduleFieldOrder = revitSchedule.Definition.GetFieldOrder();
      var numHiddenFields = 0;

      for (var columnIndex = 0; columnIndex < scheduleFieldOrder.Count; columnIndex++)
      {
        var field = revitSchedule.Definition.GetField(scheduleFieldOrder[columnIndex]);

        // we cannot get the values for hidden fields, so we need to subtract one from the index that is passed to
        // tableView.GetCellText.
        if (field.IsHidden)
        {
          numHiddenFields++;
          continue;
        }

        yield return new ScheduleColumnIterationInfo
        {
          field = field,
          columnIndex = columnIndex,
          columnCount = scheduleFieldOrder.Count,
          numHiddenFields = numHiddenFields
        };
      }
    }
  }
}
