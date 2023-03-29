using System;
using System.Collections.Generic;
using System.Text;
using Autodesk.Revit.DB;
using Objects.BuiltElements;
using Objects.Organization;
using Speckle.Core.Models;
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

      return appObj;
    }
    private DataTable ScheduleToSpeckle(DB.ViewSchedule revitSchedule)
    {
      var table = revitSchedule.GetTableData();
      DataTable speckleTable = null;

      foreach (SectionType tableSection in Enum.GetValues(typeof(SectionType)))
      {
        var section = table.GetSectionData(tableSection);

        if (section == null)
        {
          continue;
        }
        var rowCount = section.NumberOfRows;
        var columnCount = section.NumberOfColumns;

        speckleTable ??= new DataTable(columnCount);

        for (var rowIndex = 0; rowIndex < rowCount; rowIndex++)
        {
          var rowData = new List<string>();
          for (var columnIndex = 0; columnIndex < columnCount; columnIndex++)
          {
            rowData.Add(revitSchedule.GetCellText(tableSection, rowIndex, columnIndex));
          }
          speckleTable.AddRow(rowData.ToArray());
        }
      }
      
      return speckleTable;
    }
  }
}
