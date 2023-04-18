using System;
using Autodesk.Revit.DB;
using DB = Autodesk.Revit.DB;
using System.Collections.Generic;
using Objects.Converter.Revit;
using Xunit;
using System.Threading.Tasks;
using Autodesk.Revit.DB.Visual;
using System.Linq;
using System.Threading;
using Revit.Async;
using System.Diagnostics;

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
      await SpeckleToNative<DB.ViewSchedule>(AssertSchedulesEqual);
    }

    [Fact]
    [Trait("Schedule", "ToNativeUpdates")]
    public async Task ScheduleToNativeUpdates()
    {
      await SpeckleToNativeUpdates<DB.ViewSchedule>(AssertSchedulesEqual);
    }

    #endregion

    internal async Task AssertSchedulesEqual(DB.ViewSchedule sourceElem, DB.ViewSchedule destElem)
    {
      Assert.NotNull(destElem);

      var sourceValueList = await RevitTask.RunAsync(app => {
        return GetTextValuesFromSchedule(sourceElem);
      }).ConfigureAwait(false);

      var destValueList = await RevitTask.RunAsync(app => {
        return GetTextValuesFromSchedule(destElem);
      }).ConfigureAwait(false);

      var index = 0;
      for (var i = 0; i < sourceValueList.Count; i++)
      {
        for (var j = 0; j < sourceValueList[i].Count; j++)
        {
          Debug.Write(sourceValueList[i][j]);
        }
        for (var j = 0; j < sourceValueList[i].Count; j++)
        {
          Debug.Write(destValueList[i][j]);
        }
        Debug.WriteLine("");
        
        try
        {
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
      Debug.WriteLine(revitSchedule.Document.PathName);
      var values = new List<List<string>>();
      foreach (var rowInfo in RevitScheduleUtils.ScheduleRowIteration(revitSchedule))
      {
        if (rowInfo.tableSection == SectionType.Header)
        {
          continue;
        }
        var innerList = new List<string>();
        Debug.Write(rowInfo.rowIndex);
        for (var columnIndex = 0; columnIndex < rowInfo.columnCount; columnIndex++)
        {
          innerList.Add(revitSchedule.GetCellText(rowInfo.tableSection, rowInfo.rowIndex, columnIndex));
          Debug.Write(innerList[innerList.Count - 1]);
        }
        Debug.WriteLine("");
        values.Add(innerList);
      }

      return values;
    }
  }
}
