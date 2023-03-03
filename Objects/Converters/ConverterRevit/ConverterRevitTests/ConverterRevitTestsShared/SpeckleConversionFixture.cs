using System;
using Autodesk.Revit.DB;
using DB = Autodesk.Revit.DB;
using System.Collections.Generic;
using xUnitRevitUtils;

namespace ConverterRevitTests
{

  public class SpeckleConversionFixture : IDisposable
  {
    public Document SourceDoc { get; set; }
    public Document UpdatedDoc { get; set; }
    public Document NewDoc { get; set; }

    public IList<DB.Element> RevitElements { get; set; }
    public IList<DB.Element> UpdatedRevitElements { get; set; }
    public List<DB.Element> Selection { get; set; }

    public string TemplateFile => Globals.GetTestModel("template.rte");

    public virtual string TestFile { get; }
    public virtual string UpdatedTestFile { get; }
    public virtual string NewFile { get; }
    public virtual List<BuiltInCategory> Categories { get; }

    public SpeckleConversionFixture()
    {
      ElementMulticategoryFilter filter = new ElementMulticategoryFilter(Categories);

      //get selection before opening docs, if any
      Selection = xru.GetActiveSelection();
      SourceDoc = xru.OpenDoc(TestFile);

      if (UpdatedTestFile != null)
      {
        UpdatedDoc = xru.OpenDoc(UpdatedTestFile);
        UpdatedRevitElements = new FilteredElementCollector(UpdatedDoc).WhereElementIsNotElementType().WherePasses(filter).ToElements();

      }


      if (NewFile != null)
        NewDoc = xru.CreateNewDoc(TemplateFile, NewFile);

      RevitElements = new FilteredElementCollector(SourceDoc).WhereElementIsNotElementType().WherePasses(filter).ToElements();
    }

    public void Dispose()
    {
      //xru.CloseDoc(SourceDoc);
      //xru.CloseDoc(NewDoc);
    }
  }
}
