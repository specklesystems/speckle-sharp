using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Speckle.Newtonsoft.Json;
using Xunit;
using xUnitRevitUtils;
using DB = Autodesk.Revit.DB;

namespace ConverterRevitTests
{
  public abstract class SpeckleConversionFixture : IAsyncLifetime
  {
    public Document SourceDoc { get; set; }
    public Document UpdatedDoc { get; set; }
    public Document NewDoc { get; set; }
    public IList<DB.Element> RevitElements { get; set; }
    public IList<DB.Element> UpdatedRevitElements { get; set; }
    public List<DB.Element> Selection { get; set; }
    public string TemplateFile => Globals.GetTestModel("template.rte");
    public bool UpdateTestRunning { get; set; }
    public string TestClassName { get; set; }
    public virtual string TestName { get; }
    public abstract string Category { get; }
    public virtual string TestFile => Globals.GetTestModelOfCategory(Category, $"{TestName}.rvt");
    public virtual string UpdatedTestFile => Globals.GetTestModelOfCategory(Category, $"{TestName}Updated.rvt");
    public virtual string NewFile => Globals.GetTestModelOfCategory(Category, $"{TestName}ToNative.rvt");
    public virtual string ExpectedFailuresFile { get; }
    public SpeckleConversionFixture()
    {
    }

    public void Initialize()
    {
      if (!TestCategories.CategoriesDict.TryGetValue(Category.ToLower(), out var categories))
      {
        throw new System.Exception($"Category, {Category.ToLower()} is not a recognized category");
      }
      ElementMulticategoryFilter filter = new ElementMulticategoryFilter(categories);

      //get selection before opening docs, if any
      Selection = xru.GetActiveSelection().ToList();
      SourceDoc = xru.OpenDoc(TestFile);

      if (File.Exists(UpdatedTestFile))
      {
        UpdatedDoc = xru.OpenDoc(UpdatedTestFile);
        UpdatedRevitElements = new FilteredElementCollector(UpdatedDoc).WhereElementIsNotElementType().WherePasses(filter).ToElements();
      }

      if (File.Exists(NewFile))
      {
        NewDoc = xru.OpenDoc(NewFile);
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
        //var testsFailed = xru.MainViewModel.FilteredTestCases
        //  .Where(testCase => testCase.DisplayName.Contains(TestClassName))
        //  .Any(testCase => testCase.State == Xunit.Runner.Wpf.TestState.Failed);

        //// if none of the tests failed, close the documents
        //if (!testsFailed)
        //{
        xru.OpenDoc(Globals.GetTestModel("blank.rvt"));
        xru.CloseDoc(SourceDoc);
        xru.CloseDoc(UpdatedDoc);
        xru.CloseDoc(NewDoc);
        //}

        return Task.CompletedTask;
      }
      finally
      {
        SpeckleUtils.Throttler.Release();
      }
    }
  }
}
