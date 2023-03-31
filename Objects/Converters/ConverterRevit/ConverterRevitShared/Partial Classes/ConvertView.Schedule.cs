using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Autodesk.Revit.DB;
using Objects.BuiltElements;
using Objects.Organization;
using Speckle.Core.Models;
using Speckle.netDxf.Tables;
using static System.Collections.Specialized.BitVector32;
using DB = Autodesk.Revit.DB;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
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

      foreach (var row in speckleTable.Rows)
      {
        Debug.WriteLine(row.Metadata.FirstOrDefault().Value);
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

      var table = revitSchedule.GetTableData();
      var section = table.GetSectionData(SectionType.Body);
      for (var columnIndex = 0; columnIndex < section.NumberOfColumns; columnIndex++)
      {
        speckleTable.DefineColumn<string>(out var _);
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
          catch (Exception ex)
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
      var metadata = new Dictionary<string, object>()
      {
        {
          "RevitApplicationIds",
          ElementApplicationIdsInRow(rowIndex, section, originalTableIds.ToList(), revitSchedule)
        }
      };
      speckleTable.AddRow(metadata: metadata, objects: rowData.ToArray());
    }

    private List<string> ElementApplicationIdsInRow(int rowNumber, TableSectionData section, List<ElementId> orginialTableIds, DB.ViewSchedule revitSchedule)
    {
      var elementApplicationIdsInRow = new List<string>();
      List<ElementId> remainingIdsInRow = null;

      using (var t = new Transaction(Doc, "This Transaction Will Never Get Committed"))
      {
        try
        {
          t.Start();

          using var st = new SubTransaction(Doc);
          st.Start();
          section.RemoveRow(rowNumber);
          st.Commit();

          remainingIdsInRow = new FilteredElementCollector(Doc, revitSchedule.Id)
            .ToElementIds()
            .ToList();
        }
        catch (Autodesk.Revit.Exceptions.ArgumentException e)
        {
          // trying to delete a necessary row. Just ignore and move on
        }
        catch (Exception e)
        {

        }
        finally
        {
          t.RollBack();
        }
      }

      //using (var t = new Transaction(Doc, "This Transaction Will Never Get Committed"))
      //{
      //  t.Start();
      //  Doc.Regenerate();
      //  t.RollBack();
      //}

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
