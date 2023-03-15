using Autodesk.Revit.DB;
using DB = Autodesk.Revit.DB;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using xUnitRevitUtils;
using System.IO;

namespace ConverterRevitTests
{
  public class SpeckleConversionFixture : IAsyncLifetime
  {
    public Document SourceDoc { get; set; }
    public Document UpdatedDoc { get; set; }
    public Document NewDoc { get; set; }
    public IList<DB.Element> RevitElements { get; set; }
    public IList<DB.Element> UpdatedRevitElements { get; set; }
    public List<DB.Element> Selection { get; set; }
    public string TemplateFile => Globals.GetTestModel("template.rte");
    public bool UpdateTestRunning { get; set; } = false;
    public string TestClassName { get; set; }

    public virtual string TestFile { get; }
    public virtual string UpdatedTestFile { get; }
    public virtual string NewFile { get; }
    public virtual List<BuiltInCategory> Categories { get; }

    public SpeckleConversionFixture()
    {
    }

    public void Initialize()
    {
      ElementMulticategoryFilter filter = new ElementMulticategoryFilter(Categories);

      //get selection before opening docs, if any
      Selection = xru.GetActiveSelection().ToList();
      SourceDoc = xru.OpenDoc(TestFile);

      if (UpdatedTestFile != null)
      {
        UpdatedDoc = xru.OpenDoc(UpdatedTestFile);
        UpdatedRevitElements = new FilteredElementCollector(UpdatedDoc).WhereElementIsNotElementType().WherePasses(filter).ToElements();
      }

      if (NewFile != null)
      {
        if (File.Exists(NewFile))
          NewDoc = xru.OpenDoc(NewFile);
        else
          NewDoc = xru.CreateNewDoc(TemplateFile, NewFile);
      }

      RevitElements = new FilteredElementCollector(SourceDoc).WhereElementIsNotElementType().WherePasses(filter).ToElements();
    }

    public async Task InitializeAsync()
    {
      await SpeckleUtils.Throttler.WaitAsync();
      Initialize();
    }

    public Task DisposeAsync()
    {
      try
      {
        var testsFailed = xru.MainViewModel.FilteredTestCases
          .Where(testCase => testCase.DisplayName.Contains(TestClassName))
          .Any(testCase => testCase.State == Xunit.Runner.Wpf.TestState.Failed);

        // if none of the tests failed, close the documents
        if (!testsFailed)
        {
          xru.OpenDoc(Globals.GetTestModel("blank.rvt"));
          xru.CloseDoc(SourceDoc);
          xru.CloseDoc(UpdatedDoc);
          xru.CloseDoc(NewDoc);
        }

        return Task.CompletedTask;
      }
      finally
      {
        SpeckleUtils.Throttler.Release();
      }
    }
  }
}
