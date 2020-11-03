using System;
using Autodesk.Revit.DB;
using DB = Autodesk.Revit.DB;
using System.Collections.Generic;
using xUnitRevitUtils;

namespace ConverterRevitTests
{

  public class SpeckleConversionFixture : IDisposable
  {
    public Document Doc { get; set; }
    public Document NewDoc { get; set; }

    public IList<DB.Element> RevitElements { get; set; }
    public List<DB.Element> Selection { get; set; }

    public string TemplateFile => Globals.GetTestModel("template.rte");

    public virtual string TestFile { get; }
    public virtual string NewFile { get; }
    public virtual List<BuiltInCategory> Categories { get; }

    public SpeckleConversionFixture( )
    {
      ElementMulticategoryFilter filter = new ElementMulticategoryFilter(Categories);

      //get selection before opening docs, if any
      Selection = xru.GetActiveSelection();
      Doc = xru.OpenDoc(TestFile);
      if(NewFile!=null)
        NewDoc = xru.CreateNewDoc(TemplateFile, NewFile);

      RevitElements = new FilteredElementCollector(Doc).WhereElementIsNotElementType().WherePasses(filter).ToElements();
    }

    public void Dispose()
    {
      //xru.CloseDoc(Doc);
      //xru.CloseDoc(NewDoc);
    }
  }
}
