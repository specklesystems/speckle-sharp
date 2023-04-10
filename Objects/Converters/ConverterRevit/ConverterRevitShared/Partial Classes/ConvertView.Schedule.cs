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
          System.Diagnostics.Debug.WriteLine($"");
          for (var columnIndex = 0; columnIndex< columnCount; columnIndex++)
          {
            string existingValue = "";
            try
            {
              existingValue = revitSchedule.GetCellText(tableSection, rowIndex, columnIndex);
            }
            catch { }
            System.Diagnostics.Debug.Write($"{existingValue}, ");
          }
          System.Diagnostics.Debug.Write($"{rowIndex}");

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

      //for (var columnIndex = 0; columnIndex < speckleTable.columnCount; columnIndex++)
      //{
      //  var columnMetadata = speckleTable.columnMetadata[columnIndex];

      //  if (!(columnMetadata is Base @base && @base["BuiltInParameterInteger"] is long paramId))
      //    throw new Exception("Metadata got messed up in this process");

      //  var param = (BuiltInParameter)paramId;
      //  for (var rowIndex = 0; rowIndex < speckleTable.rowCount; rowIndex++)
      //  {
      //    revitSchedule.GetCellText(SectionType.Body, rowIndex, columnIndex);
      //    if (speckleTable.rowMetadata[rowIndex]["RevitApplicationIds"] is IList idList)
      //    {
      //      foreach (var id in idList)
      //      {
      //        var element = Doc.GetElement(id.ToString());
      //        if (element == null)
      //          continue;

      //        TrySetParam(element, param, speckleTable.data[rowIndex][columnIndex]);
      //      }
      //    }
      //  }
      //}

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
            AddRowToSpeckleTable(
              revitSchedule,
              speckleTable,
              originalTableIds,
              tableSection,
              section,
              columnCount,
              rowIndex
            );
          }
          catch (Autodesk.Revit.Exceptions.ArgumentOutOfRangeException ex)
          {
          }
        }
      }
      
      return speckleTable;
    }

    private void AddRowToSpeckleTable(ViewSchedule revitSchedule, DataTable speckleTable, ICollection<ElementId> originalTableIds, SectionType tableSection, TableSectionData section, int columnCount, int rowIndex)
    {
      var rowData = new List<string>();
      for (var columnIndex = 0; columnIndex < columnCount; columnIndex++)
      {
        rowData.Add(revitSchedule.GetCellText(tableSection, rowIndex, columnIndex));
      }

      if (!rowData.Where(s => !string.IsNullOrEmpty(s)).Any())
      {
        return;
      }
      var metadata = new Base();
      metadata["RevitApplicationIds"] = ElementApplicationIdsInRow(rowIndex, section, originalTableIds, revitSchedule, tableSection);
      speckleTable.AddRow(metadata: metadata, objects: rowData.ToArray());
    }

    private List<string> ElementApplicationIdsInRow(int rowNumber, TableSectionData section, ICollection<ElementId> orginialTableIds, DB.ViewSchedule revitSchedule, SectionType tableSection)
    {
      var elementApplicationIdsInRow = new List<string>();
      List<ElementId> remainingIdsInRow = null;

      if (!Doc.IsModifiable)
      {
        using var t = new Transaction(Doc, "This Transaction Will Never Get Committed");
        try
        {
          t.Start();
          section.RemoveRow(rowNumber);
          remainingIdsInRow = new FilteredElementCollector(Doc, revitSchedule.Id)
            .ToElementIds()
            .ToList();
        }
        catch
        {
          // trying to delete a necessary row.
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
          section.RemoveRow(rowNumber);
          remainingIdsInRow = new FilteredElementCollector(Doc, revitSchedule.Id)
            .ToElementIds()
            .ToList();
        }
        catch
        {
          // trying to delete a necessary row.
          // ignore because we're just going to rollback
        }
        finally
        {
          t.RollBack();
        }
      }

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
  }
}
