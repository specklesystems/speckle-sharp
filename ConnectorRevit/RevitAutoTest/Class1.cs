using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RevitAutoTest
{
  public class Class1 : IExternalApplication
  {
    public Result OnStartup(UIControlledApplication application)
    {
      TaskDialog.Show("Title", "Hello World!");

      application.Idling += (sender, args) =>
      {
        UIApplication uiApp = sender as UIApplication;
        Document doc = uiApp.ActiveUIDocument.Document;

        using (Transaction transaction = new Transaction(doc, "Text Note Update"))
        {
          transaction.Start();

          transaction.Commit();
        }
      };
      return Result.Succeeded;
    }

    public Result OnShutdown(UIControlledApplication application)
    {
      throw new NotImplementedException();
    }
  }
}
