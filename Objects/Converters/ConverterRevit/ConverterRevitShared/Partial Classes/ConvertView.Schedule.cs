using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Objects.Organization;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using DB = Autodesk.Revit.DB;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    #region ToNative
    struct RevitScheduleData
    {
      public int ColumnIndex;
      public BuiltInParameter Parameter;
    }
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
        throw new Exception($"Existing element with UniqueId = {docObj.UniqueId} is of the type {docObj.GetType()}, not of the expected type, DB.ViewSchedule");
      }

      var speckleIndexToRevitScheduleDataMap = new Dictionary<int, RevitScheduleData>();
      var paramsToPass = new ScheduleColumnParameters()
      {
        speckleIndexToRevitScheduleDataMap = speckleIndexToRevitScheduleDataMap
      };
      ForEachColumnInSchedule(AddToIndexToScheduleMap, revitSchedule, speckleTable, paramsToPass);


      var originalTableIds = new FilteredElementCollector(Doc, revitSchedule.Id)
        .ToElementIds();
      var parametersToPass = new ScheduleRowParameters()
      {
        speckleTable = speckleTable,
        originalTableIds = originalTableIds,
        speckleIndexToRevitScheduleDataMap = speckleIndexToRevitScheduleDataMap
      };

      ForEachRowInSchedule(UpdateDataInRow, revitSchedule, parametersToPass);

      return appObj;
    }

    private static (loopStatus, bool) AddToIndexToScheduleMap(ScheduleColumnParameters parameters)
    {
      var fieldInt = parameters.field.ParameterId.IntegerValue;
      var incomingColumnIndex = parameters.speckleTable.columnMetadata
        .FindIndex(b => b["BuiltInParameterInteger"] is long paramInt && paramInt == fieldInt);

      if (incomingColumnIndex == -1)
      {
        return (loopStatus.Continue, false);
      }

      var scheduleData = new RevitScheduleData
      {
        ColumnIndex = parameters.columnIndex - parameters.numHiddenFields,
        Parameter = (BuiltInParameter)fieldInt
      };

      parameters.speckleIndexToRevitScheduleDataMap.Add(incomingColumnIndex, scheduleData);
      return (loopStatus.Continue, false);
    }

    private (loopStatus, bool) UpdateDataInRow(ScheduleRowParameters parameters)
    {
      var elementIds = ElementApplicationIdsInRow(parameters.rowIndex, parameters.section, parameters.originalTableIds, parameters.revitSchedule, parameters.tableSection);

      foreach (var id in elementIds)
      {
        var speckleObjectRowIndex = parameters.speckleTable.rowMetadata
          .FindIndex(b => b["RevitApplicationIds"] is IList list && list.Contains(id));

        if (speckleObjectRowIndex == -1)
        {
          continue;
        }

        foreach (var kvp in parameters.speckleIndexToRevitScheduleDataMap)
        {
          var speckleObjectColumnIndex = kvp.Key;
          var revitScheduleData = kvp.Value;
          var existingValue = parameters.revitSchedule.GetCellText(parameters.tableSection, parameters.rowIndex, revitScheduleData.ColumnIndex);
          var newValue = parameters.speckleTable.data[speckleObjectRowIndex][speckleObjectColumnIndex];
          if (existingValue == newValue.ToString())
          {
            continue;
          }

          var element = Doc.GetElement(id);
          if (element == null)
            continue;

          TrySetParam(element, revitScheduleData.Parameter, newValue, "none");
        }
      }
      return (loopStatus.Continue, false);
    }

    #endregion

    #region ToSpeckle
    private DataTable ScheduleToSpeckle(DB.ViewSchedule revitSchedule)
    {
      var speckleTable = new DataTable
      {
        applicationId = revitSchedule.UniqueId
      };

      var originalTableIds = new FilteredElementCollector(Doc, revitSchedule.Id)
          .ToElementIds();

      var skippedIndicies = new Dictionary<SectionType, List<int>>();
      var columnHeaders = new List<string>();

      DefineColumnMetadata(revitSchedule, speckleTable, originalTableIds, columnHeaders);
      PopulateDataTableRows(revitSchedule, speckleTable, originalTableIds, skippedIndicies);

      speckleTable.headerRowIndex = Math.Max(0, GetTableHeaderIndex(revitSchedule, skippedIndicies, columnHeaders.FirstOrDefault()));

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

    private void DefineColumnMetadata(ViewSchedule revitSchedule, DataTable speckleTable, ICollection<ElementId> originalTableIds, List<string> columnHeaderList)
    {
      Element firstElement = null;
      Element firstType = null;
      if (originalTableIds.Count > 0)
      {
        firstElement = Doc.GetElement(originalTableIds.First());
        firstType = Doc.GetElement(firstElement.GetTypeId());
      }

      var paramsToPass = new ScheduleColumnParameters()
      {
        columnHeaders = columnHeaderList,
        firstElement= firstElement,
        firstType=firstType,
      };

      ForEachColumnInSchedule(AddColumnMetadataToDataTable, revitSchedule, speckleTable, paramsToPass);
    }

    private static (loopStatus, bool) AddColumnMetadataToDataTable(ScheduleColumnParameters parameters)
    {
      // add column header to list for potential future use
      parameters.columnHeaders.Add(parameters.field.ColumnHeading);

      var builtInParameter = (BuiltInParameter)parameters.field.ParameterId.IntegerValue;

      var columnMetadata = new Base();
      columnMetadata["BuiltInParameterInteger"] = parameters.field.ParameterId.IntegerValue;
      columnMetadata["FieldType"] = parameters.field.FieldType.ToString();

      Parameter param;
      if (parameters.field.FieldType == ScheduleFieldType.ElementType)
      {
        if (parameters.firstType != null)
        {
          param = parameters.firstType.get_Parameter(builtInParameter);
          columnMetadata["IsReadOnly"] = param?.IsReadOnly;
        }
      }
      else if (parameters.field.FieldType == ScheduleFieldType.Instance)
      {
        if (parameters.firstElement != null)
        {
          param = parameters.firstElement.get_Parameter(builtInParameter);
          columnMetadata["IsReadOnly"] = param?.IsReadOnly;
        }
      }
      else
      {
        var scheduleCategory = (BuiltInCategory)parameters.revitSchedule.Definition.CategoryId.IntegerValue;
        SpeckleLog.Logger.Warning("Schedule of category, {scheduleCategory}, contains field of type {builtInParameter} which has an unsupported field type, {fieldType}",
          scheduleCategory,
          builtInParameter,
          parameters.field.FieldType.ToString());
      }
      parameters.speckleTable.DefineColumn(columnMetadata);
      return (loopStatus.Continue, false);
    }

    private void PopulateDataTableRows(ViewSchedule revitSchedule, DataTable speckleTable, ICollection<ElementId> originalTableIds, Dictionary<SectionType, List<int>> skippedIndicies)
    {
      var parametersToPass = new ScheduleRowParameters()
      {
        speckleTable = speckleTable,
        originalTableIds = originalTableIds,
        skippedIndicies = skippedIndicies
      };

      ForEachRowInSchedule<bool>((parameters) =>
      {
        try
        {
          var rowAdded = AddRowToSpeckleTable(
            parameters.revitSchedule,
            parameters.speckleTable,
            parameters.originalTableIds,
            parameters.tableSection,
            parameters.section,
            parameters.columnCount,
            parameters.rowIndex
          );
          if (!rowAdded)
          {
            if (!parameters.skippedIndicies.ContainsKey(parameters.tableSection))
            {
              skippedIndicies.Add(parameters.tableSection, new List<int>());
            }
            skippedIndicies[parameters.tableSection].Add(parameters.rowIndex);
          }
        }
        catch (Autodesk.Revit.Exceptions.ArgumentOutOfRangeException ex)
        {
        }

        return (loopStatus.Continue, false);
      }, revitSchedule, parametersToPass);
    }

    private int GetTableHeaderIndex(ViewSchedule revitSchedule, Dictionary<SectionType, List<int>> skippedIndicies, string firstColumnHeader)
    {
      var hasHeaders = revitSchedule.Definition.ShowHeaders;

      // TODO: figure out what to do if the table doesn't have headers
      if (!hasHeaders)
      {
        return ExecuteInTemporaryTransaction(() =>
        {
          revitSchedule.Definition.ShowHeaders = true;
          return GetHeaderIndexFromScheduleWithHeaders(revitSchedule, skippedIndicies, firstColumnHeader);
        });
      }

      return GetHeaderIndexFromScheduleWithHeaders(revitSchedule, skippedIndicies, firstColumnHeader);
    }

    private static int GetHeaderIndexFromScheduleWithHeaders(ViewSchedule revitSchedule, Dictionary<SectionType, List<int>> skippedIndicies, string firstColumnHeader)
    {
      var parametersToPass = new ScheduleRowParameters()
      {
        firstColumnHeading= firstColumnHeader,
        skippedIndicies = skippedIndicies
      };

      return ForEachRowInSchedule((parameters) =>
      {
        var cellValue = parameters.revitSchedule.GetCellText(parameters.tableSection, parameters.rowIndex, 0);
        if (cellValue != parameters.firstColumnHeading)
        {
          return (loopStatus.Continue, 0);
        }
        return (loopStatus.Return, parameters.masterRowIndex);
      }, revitSchedule, parametersToPass);
    }

    private bool AddRowToSpeckleTable(ViewSchedule revitSchedule, DataTable speckleTable, ICollection<ElementId> originalTableIds, SectionType tableSection, TableSectionData section, int columnCount, int rowIndex)
    {
      var rowData = new List<string>();
      for (var columnIndex = 0; columnIndex < columnCount; columnIndex++)
      {
        rowData.Add(revitSchedule.GetCellText(tableSection, rowIndex, columnIndex));
      }

      if (!rowData.Where(s => !string.IsNullOrEmpty(s)).Any())
      {
        return false;
      }
      var metadata = new Base();
      metadata["RevitApplicationIds"] = ElementApplicationIdsInRow(rowIndex, section, originalTableIds, revitSchedule, tableSection);

      try
      {
        speckleTable.AddRow(metadata: metadata, objects: rowData.ToArray());
      }
      catch (ArgumentException)
      {
        // trying to add an invalid row. Just don't add it and continue to the next
        return false;
      }

      return true;
    }
    #endregion

    private List<string> ElementApplicationIdsInRow(int rowNumber, TableSectionData section, ICollection<ElementId> orginialTableIds, DB.ViewSchedule revitSchedule, SectionType tableSection)
    {
      var elementApplicationIdsInRow = new List<string>();
      var remainingIdsInRow = ExecuteInTemporaryTransaction(() =>
      {
        section.RemoveRow(rowNumber);
        return new FilteredElementCollector(Doc, revitSchedule.Id)
          .ToElementIds()
          .ToList();
      });

      // the section must be recomputed here because of our hacky row deleting trick
      var table = revitSchedule.GetTableData();
      section = table.GetSectionData(tableSection);

      if (remainingIdsInRow == null || remainingIdsInRow.Count == orginialTableIds.Count)
        return elementApplicationIdsInRow;

      foreach (var id in orginialTableIds)
      {
        if (remainingIdsInRow.Contains(id)) continue;
        elementApplicationIdsInRow.Add(Doc.GetElement(id).UniqueId);
      }

      return elementApplicationIdsInRow;
    }

    private TResult ExecuteInTemporaryTransaction<TResult>(Func<TResult> function)
    {
      TResult result = default;
      if (!Doc.IsModifiable)
      {
        using var t = new Transaction(Doc, "This Transaction Will Never Get Committed");
        try
        {
          t.Start();
          result = function();
        }
        catch
        {
          // ignore because we're just going to rollback
        }
        finally
        {
          t.RollBack();
        }
      }
      else
      {
        using var t = new SubTransaction(Doc);
        try
        {
          t.Start();
          result = function();
        }
        catch
        {
          // ignore because we're just going to rollback
        }
        finally
        {
          t.RollBack();
        }
      }

      return result;
    }

    private static TResult ForEachRowInSchedule<TResult>(
      Func<ScheduleRowParameters, (loopStatus, TResult)> function,
      ViewSchedule revitSchedule,
      ScheduleRowParameters parameters)
    {
      parameters.revitSchedule = revitSchedule;
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

        parameters.rowCount= rowCount;
        parameters.columnCount = columnCount;
        parameters.tableSection= tableSection;
        parameters.section= section;

        for (var rowIndex = 0; rowIndex < rowCount; rowIndex++)
        {
          parameters.rowIndex = rowIndex;
          parameters.masterRowIndex = masterRowIndex;

          (loopStatus, TResult) result = function(parameters);

          if (parameters.skippedIndicies == null || !parameters.skippedIndicies.TryGetValue(tableSection, out var indicies) || !indicies.Contains(rowIndex))
          {
            // this "skippedIndicies" dict contains the indicies that contain only empty values
            // these values were skipped when adding them to the DataTable, so the indicies of the revitSchedule
            // and the Speckle DataTable will differ at these indicies (and all subsequent indicies)

            // therefore we only want to increment the masterRowIndex if this row was added to the Speckle DataTable
            masterRowIndex++;
          }

          if (result.Item1 == loopStatus.Continue)
          {
            continue;
          }
          else if (result.Item1 == loopStatus.Break)
          {
            break;
          }
          else
          {
            return result.Item2;
          }
        }
      }
      return default;
    }

    private enum loopStatus
    {
      Continue,
      Break,
      Return
    }

    private class ScheduleRowParameters
    {
      public ViewSchedule revitSchedule;
      public SectionType tableSection;
      public TableSectionData section;
      public int rowIndex;
      public int rowCount;
      public int columnCount;
      public int masterRowIndex;

      public DataTable speckleTable;
      public string firstColumnHeading;
      public ICollection<ElementId> originalTableIds;
      public Dictionary<int, RevitScheduleData> speckleIndexToRevitScheduleDataMap;
      public Dictionary<SectionType, List<int>> skippedIndicies;
    }

    private static TResult ForEachColumnInSchedule<TResult>(
      Func<ScheduleColumnParameters, (loopStatus, TResult)> function,
      ViewSchedule revitSchedule,
      DataTable speckleTable,
      ScheduleColumnParameters parameters)
    {
      var scheduleFieldOrder = revitSchedule.Definition.GetFieldOrder();
      var numHiddenFields = 0;

      parameters.revitSchedule = revitSchedule;
      parameters.speckleTable = speckleTable;
      parameters.columnCount = scheduleFieldOrder.Count;

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

        parameters.field = field;
        parameters.columnIndex = columnIndex;
        parameters.numHiddenFields = numHiddenFields;

        (loopStatus, TResult) result = function(parameters);

        if (result.Item1 == loopStatus.Continue)
        {
          continue;
        }
        else if (result.Item1 == loopStatus.Break)
        {
          break;
        }
        else
        {
          return result.Item2;
        }
      }

      return default;
    }
    
    private class ScheduleColumnParameters
    {
      public ViewSchedule revitSchedule;
      public DataTable speckleTable;
      public ScheduleField field;
      public int columnIndex;
      public int columnCount;
      public int numHiddenFields;
      public Element firstElement;
      public Element firstType;
      public List<string> columnHeaders;
      public Dictionary<int, RevitScheduleData> speckleIndexToRevitScheduleDataMap;
    }
  }
}
