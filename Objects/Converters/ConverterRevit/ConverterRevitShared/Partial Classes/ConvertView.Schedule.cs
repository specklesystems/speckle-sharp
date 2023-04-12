using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Objects.Organization;
using Speckle.Core.Models;
using DB = Autodesk.Revit.DB;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
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

      TableData table;
      TableSectionData section;
      var originalTableIds = new FilteredElementCollector(Doc, revitSchedule.Id)
          .ToElementIds();

      var speckleIndexToRevitScheduleDataMap = new Dictionary<int, RevitScheduleData>();

      var scheduleFieldOrder = revitSchedule.Definition.GetFieldOrder();

      for (var i = 0; i < scheduleFieldOrder.Count; i++)
      {
        var field = revitSchedule.Definition.GetField(scheduleFieldOrder[i]);
        var fieldInt = field.ParameterId.IntegerValue;
        var incomingColumnIndex = speckleTable.columnMetadata
          .FindIndex(b => b["BuiltInParameterInteger"] is long paramInt && paramInt == fieldInt);

        if (incomingColumnIndex == -1)
        {
          continue;
        }

        var scheduleData = new RevitScheduleData
        {
          ColumnIndex = i,
          Parameter = (BuiltInParameter)fieldInt
        };

        speckleIndexToRevitScheduleDataMap.Add(incomingColumnIndex, scheduleData);
      }

      foreach (SectionType tableSection in Enum.GetValues(typeof(SectionType)))
      {
        // the table must be recomputed here because of our hacky row deleting trick
        table = revitSchedule.GetTableData();

        section = table.GetSectionData(tableSection);

        if (section == null)
        {
          continue;
        }
        var rowCount = section.NumberOfRows;
        var columnCount = section.NumberOfColumns;

        for (var rowIndex = 0; rowIndex < rowCount; rowIndex++)
        {
          for (var columnIndex = 0; columnIndex< columnCount; columnIndex++)
          {
            string existingValue = "";
            try
            {
              existingValue = revitSchedule.GetCellText(tableSection, rowIndex, columnIndex);
            }
            catch { }
          }

          var elementIds = ElementApplicationIdsInRow(rowIndex, section, originalTableIds, revitSchedule, tableSection);

          foreach (var id in elementIds)
          {
            var speckleObjectRowIndex = speckleTable.rowMetadata
              .FindIndex(b => b["RevitApplicationIds"] is IList list && list.Contains(id));

            if (speckleObjectRowIndex == -1)
            {
              continue;
            }

            foreach (var kvp in speckleIndexToRevitScheduleDataMap)
            {
              var speckleObjectColumnIndex = kvp.Key;
              var revitScheduleData = kvp.Value;
              var existingValue = revitSchedule.GetCellText(tableSection, rowIndex, revitScheduleData.ColumnIndex);
              var newValue = speckleTable.data[speckleObjectRowIndex][speckleObjectColumnIndex];
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
        } 
      }

      return appObj;
    }
    private DataTable ScheduleToSpeckle(DB.ViewSchedule revitSchedule)
    {
      var speckleTable = new DataTable
      {
        applicationId = revitSchedule.UniqueId
      };

      var originalTableIds = new FilteredElementCollector(Doc, revitSchedule.Id)
          .ToElementIds();

      var skippedIndicies = new Dictionary<SectionType, List<int>>();

      DefineColumnMetadata(revitSchedule, speckleTable, originalTableIds);
      PopulateDataTableRows(revitSchedule, speckleTable, originalTableIds, skippedIndicies);
      speckleTable.headerRowIndex = Math.Max(0, GetTableHeaderIndex(revitSchedule, skippedIndicies));

      return speckleTable;
    }

    private void DefineColumnMetadata(ViewSchedule revitSchedule, DataTable speckleTable, ICollection<ElementId> originalTableIds)
    {
      Element firstElement = null;
      if (originalTableIds.Count > 0)
      {
        firstElement = Doc.GetElement(originalTableIds.First());
      }

      foreach (var fieldId in revitSchedule.Definition.GetFieldOrder())
      {
        var field = revitSchedule.Definition.GetField(fieldId);

        var columnMetadata = new Base();
        columnMetadata["BuiltInParameterInteger"] = field.ParameterId.IntegerValue;

        if (firstElement != null)
        {
          var param = firstElement.get_Parameter((BuiltInParameter)field.ParameterId.IntegerValue);
          columnMetadata["IsReadOnly"] = param.IsReadOnly;
        }
        speckleTable.DefineColumn(columnMetadata);
      }
    }

    private void PopulateDataTableRows(ViewSchedule revitSchedule, DataTable speckleTable, ICollection<ElementId> originalTableIds, Dictionary<SectionType, List<int>> skippedIndicies)
    {
      TableData table;
      TableSectionData section;

      foreach (SectionType tableSection in Enum.GetValues(typeof(SectionType)))
      {
        // the table must be recomputed here because of our hacky row deleting trick
        table = revitSchedule.GetTableData();
        section = table.GetSectionData(tableSection);

        if (section == null)
        {
          continue;
        }
        var rowCount = section.NumberOfRows;
        var columnCount = section.NumberOfColumns;

        for (var rowIndex = 0; rowIndex < rowCount; rowIndex++)
        {
          try
          {
            var rowAdded = AddRowToSpeckleTable(
              revitSchedule,
              speckleTable,
              originalTableIds,
              tableSection,
              section,
              columnCount,
              rowIndex
            );
            if (!rowAdded)
            {
              if (!skippedIndicies.ContainsKey(tableSection))
              {
                skippedIndicies.Add(tableSection, new List<int>());
              }
              skippedIndicies[tableSection].Add(rowIndex);
            }
          }
          catch (Autodesk.Revit.Exceptions.ArgumentOutOfRangeException ex)
          {
          }
        }
      }
    }

    private int GetTableHeaderIndex(ViewSchedule revitSchedule, Dictionary<SectionType, List<int>> skippedIndicies)
    {
      var masterRowIndex = 0;
      int numRowsToTry = 6;
      var hasHeaders = revitSchedule.Definition.ShowHeaders;

      // TODO: figure out what to do if the table doesn't have headers
      if (!hasHeaders)
      {
        return -1;
      }

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

        var originalColumn0 = GetFirstNValues(revitSchedule, tableSection, Math.Min(rowCount, numRowsToTry));
        var modifiedColumn0 = ExecuteInTemporaryTransaction(() =>
        {
          revitSchedule.Definition.ShowHeaders = false;
          var table = revitSchedule.GetTableData();
          var section = table.GetSectionData(tableSection);
          var rowCount = section.NumberOfRows;
          return GetFirstNValues(revitSchedule, tableSection, Math.Min(rowCount, numRowsToTry));
        });

        for (var i = 0; i < modifiedColumn0.Count; i++)
        {
          if (originalColumn0[i] != modifiedColumn0[i])
          {
            return masterRowIndex;
          }

          if (!skippedIndicies.TryGetValue(tableSection, out var indicies) || !indicies.Contains(i))
          {
            // this "skippedIndicies" dict contains the indicies that contain only empty values
            // these values were skipped when adding them to the DataTable, so the indicies of the revitSchedule
            // and the Speckle DataTable will differ at these indicies (and all subsequent indicies)

            // therefore we only want to increment the masterRowIndex if this row was added to the Speckle DataTable
            masterRowIndex++;
          }
        }
      }

      return -1;
    }

    private static List<string> GetFirstNValues(ViewSchedule revitSchedule, SectionType tableSection, int rowCount)
    {
      var firstNValues = new List<string>();
      for (var rowIndex = 0; rowIndex < rowCount; rowIndex++)
      {
        firstNValues.Add(revitSchedule.GetCellText(tableSection, rowIndex, 0));
      }
      return firstNValues;
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
      speckleTable.AddRow(metadata: metadata, objects: rowData.ToArray());

      return true;
    }

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

    //private TResult LoopThroughViewSchedule<TResult>(ViewSchedule revitSchedule, Func<(bool,TResult)> function)
    //{
    //  foreach (SectionType tableSection in Enum.GetValues(typeof(SectionType)))
    //  {
    //    // the table must be recomputed here because of our hacky row deleting trick
    //    var table = revitSchedule.GetTableData();
    //    var section = table.GetSectionData(tableSection);

    //    if (section == null)
    //    {
    //      continue;
    //    }
    //    var rowCount = section.NumberOfRows;
    //    var columnCount = section.NumberOfColumns;

    //    for (var rowIndex = 0; rowIndex < rowCount; rowIndex++)
    //    {

    //    }
    //  }
    //}
  }
}
