using System;
using Autodesk.Revit.DB;
using DB = Autodesk.Revit.DB;
using System.Collections.Generic;
using Objects.Converter.Revit;
using Xunit;
using System.Threading.Tasks;
using System.Linq;
using Revit.Async;

namespace ConverterRevitTests
{
  public class ScheduleFixture : SpeckleConversionFixture
  {
    public override string TestFile => Globals.GetTestModel("Schedule.rvt");
    public override string UpdatedTestFile => Globals.GetTestModel("ScheduleUpdated.rvt");
    public override string NewFile => Globals.GetTestModel("ScheduleToNative.rvt");
    public override List<BuiltInCategory> Categories => new() { BuiltInCategory.OST_Schedules };
    public ScheduleFixture() : base ()
    {
    }
  }

  public class ScheduleTests : SpeckleConversionTest, IClassFixture<ScheduleFixture>
  {
    public ScheduleTests(ScheduleFixture fixture) : base(fixture)
    {
    }

   [Fact]
   [Trait("Schedule", "ToSpeckle")]
    public async Task ScheduleToSpeckle()
    {
      await NativeToSpeckle();
    }

    #region ToNative

    [Fact]
    [Trait("Schedule", "ToNative")]
    public async Task ScheduleToNative()
    {
      await SpeckleToNative<DB.ViewSchedule>(null, AssertSchedulesEqual);
    }

    [Fact]
    [Trait("Schedule", "ToNativeUpdates")]
    public async Task ScheduleToNativeUpdates()
    {
      await SpeckleToNativeUpdates<DB.ViewSchedule>(null, AssertSchedulesEqual);
    }

    #endregion

    internal async Task AssertSchedulesEqual(DB.ViewSchedule sourceElem, DB.ViewSchedule destElem)
    {
      AssertElementEqual(sourceElem, destElem);

      var sourceValueList = await RevitTask.RunAsync(app => {
        return GetTextValuesFromSchedule(sourceElem);
      }).ConfigureAwait(false);

      var destValueList = await RevitTask.RunAsync(app => {
        return GetTextValuesFromSchedule(destElem);
      }).ConfigureAwait(false);

      var index = 0;
      for (var i = 0; i < sourceValueList.Count; i++)
      {      
        try
        {
          // you can't unassign parameter values in Revit, so just ignore those
          var emptyIndicies = Enumerable.Range(0, sourceValueList[i].Count)
             .Where(j => string.IsNullOrWhiteSpace(sourceValueList[i][j]))
             .ToList();

          if (emptyIndicies.Any())
          {
            for (var j = emptyIndicies.Count - 1; j >= 0; j--)
            {
              sourceValueList[i].RemoveAt(emptyIndicies[j]);
              destValueList[i].RemoveAt(emptyIndicies[j]);
            }
          }
          Assert.Equal(sourceValueList[i], destValueList[i]);
        }
        catch(Exception ex)
        {
          throw;
        }
        index++;
      }
    }

    private static List<List<string>> GetTextValuesFromSchedule(ViewSchedule revitSchedule)
    {
      var values = new List<List<string>>();
      foreach (var rowInfo in RevitScheduleUtils.ScheduleRowIteration(revitSchedule))
      {
        if (rowInfo.tableSection == SectionType.Header)
        {
          continue;
        }
        var innerList = new List<string>();
        for (var columnIndex = 0; columnIndex < rowInfo.columnCount; columnIndex++)
        {
          innerList.Add(revitSchedule.GetCellText(rowInfo.tableSection, rowInfo.rowIndex, columnIndex));
        }
        values.Add(innerList);
      }

      return values;
    }
  }
}
