using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using DesktopUI2.Models;
using DesktopUI2.Models.Filters;
using Speckle.Core.Api;
using Speckle.Newtonsoft.Json;
using Xunit;
using xUnitRevitUtils;
using DB = Autodesk.Revit.DB;

namespace ConverterRevitTests
{
  public abstract class SpeckleConversionFixture : IAsyncLifetime
  {
    public UIDocument SourceDoc { get; set; }
    public UIDocument UpdatedDoc { get; set; }
    public UIDocument NewDoc { get; set; }
    public StreamState StreamState { get; } = new();
    public IList<DB.Element> RevitElements { get; set; }
    public IList<DB.Element> UpdatedRevitElements { get; set; }
    public List<DB.Element> Selection { get; set; }
    public string TemplateFile => Globals.GetTestModel("template.rte");
    public string TestClassName { get; set; }
    public virtual string TestName { get; }
    public abstract string Category { get; }
    public virtual string TestFile => Globals.GetTestModelOfCategory(Category, $"{TestName}.rvt");
    public virtual string UpdatedTestFile => Globals.GetTestModelOfCategory(Category, $"{TestName}Updated.rvt");
    public virtual string NewFile => Globals.GetTestModelOfCategory(Category, $"{TestName}ToNative.rvt");
    public Dictionary<string, Commit> Commits { get; } = new();

    public void Initialize()
    {
      if (!TestCategories.CategoriesDict.TryGetValue(Category.ToLower(), out var categories))
      {
        throw new System.Exception($"Category, {Category.ToLower()} is not a recognized category");
      }


      ElementMulticategoryFilter filter = new ElementMulticategoryFilter(categories);

      //get selection before opening docs, if any
      Selection = xru.GetActiveSelection().ToList();
      SourceDoc = SpeckleUtils.OpenUIDoc(TestFile);

      if (File.Exists(UpdatedTestFile))
      {
        UpdatedDoc = SpeckleUtils.OpenUIDoc(UpdatedTestFile);
        UpdatedRevitElements = new FilteredElementCollector(UpdatedDoc.Document).WhereElementIsNotElementType().WherePasses(filter).ToElements();
      }

      if (File.Exists(NewFile))
      {
        NewDoc = SpeckleUtils.OpenUIDoc(NewFile);
      }

      RevitElements = new FilteredElementCollector(SourceDoc.Document).WhereElementIsNotElementType().WherePasses(filter).ToElements();

      this.StreamState.Filter = new ListSelectionFilter
      {
        Slug = "category",
        Selection = categories
          .Select(cat => DB.Category.GetCategory(SourceDoc.Document, cat)?.Name)
          .Where(cat => cat != null)
          .ToList()
      };
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
        //SpeckleUtils.OpenUIDoc(Globals.GetTestModel("blank.rvt"));
        //xru.CloseDoc(SourceDoc.Document);
        //xru.CloseDoc(UpdatedDoc.Document);
        //xru.CloseDoc(NewDoc.Document);
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
