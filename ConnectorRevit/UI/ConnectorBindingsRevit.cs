using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Speckle.DesktopUI;
using Speckle.DesktopUI.Utils;

namespace Speckle.ConnectorRevit.UI
{
  public partial class ConnectorBindingsRevit : ConnectorBindings
  {
    public string TestParam = "hello from Revit bindings!";
    public static UIApplication RevitApp;
    public static UIDocument CurrentDoc { get => RevitApp.ActiveUIDocument; }
    public override void AddObjectsToSender(string args)
    {
      throw new NotImplementedException();
    }

    public override void AddReceiver(string args)
    {
      throw new NotImplementedException();
    }

    public override void AddSelectionToSender(string args)
    {
      throw new NotImplementedException();
    }

    public override void AddSender(string args)
    {
      throw new NotImplementedException();
    }

    public override void BakeReceiver(string args)
    {
      throw new NotImplementedException();
    }

    public override string GetApplicationHostName()
    {
      return "Revit";
    }

    public override string GetDocumentId()
    {
      return GetDocHash(CurrentDoc.Document);
    }

    private string GetDocHash(Document doc)
    {
      //return SpeckleCore.Converter.getMd5Hash(doc.PathName + doc.Title);
      // NOTE: If project is copy pasted, it has the same unique id, so the below is not reliable
      return CurrentDoc.Document.ProjectInformation.UniqueId;
    }

    public override string GetDocumentLocation()
    {
      return CurrentDoc.Document.PathName;
    }

    public override string GetFileClients()
    {
      throw new NotImplementedException();
    }

    public override string GetFileName()
    {
      return CurrentDoc.Document.Title;
    }

    public override List<ISelectionFilter> GetSelectionFilters()
    {

      var categories = new List<string>();
      var parameters = new List<string>();
      var views = new List<string>();
      if (CurrentDoc != null)
      {
        //selectionCount = CurrentDoc.Selection.GetElementIds().Count();
        categories = Globals.GetCategoryNames(CurrentDoc.Document);
        parameters = Globals.GetParameterNames(CurrentDoc.Document);
        views = Globals.GetViewNames(CurrentDoc.Document);
      }


      return new List<ISelectionFilter>
      {
        new ElementsSelectionFilter
        {
          Name = "Selection",
          Icon = "mouse",
          Selection = new List<string>()
        },
        new ListSelectionFilter
        {
          Name = "Category",
          Icon = "category",
          Values = categories
        },
        new ListSelectionFilter
        {
          Name = "View",
          Icon = "remove_red_eye",
          Values = views
        },
        new PropertySelectionFilter
        {
          Name = "Parameter",
          Icon = "filter_list",
          HasCustomProperty = false,
          Values = parameters,
          Operators = new List<string>
          {
            "equals",
            "contains",
            "is greater than",
            "is less than"
          }
        }
      };
    }

    public override void PushSender(string args)
    {
      throw new NotImplementedException();
    }

    public override void RemoveClient(string args)
    {
      throw new NotImplementedException();
    }

    public override void RemoveObjectsFromSender(string args)
    {
      throw new NotImplementedException();
    }

    public override void RemoveSelectionFromSender(string args)
    {
      throw new NotImplementedException();
    }

    public override void SelectClientObjects(string args)
    {
      throw new NotImplementedException();
    }

    public override void UpdateSender(string args)
    {
      throw new NotImplementedException();
    }
  }
}
