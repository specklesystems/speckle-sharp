using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Speckle.Core.Api;

namespace Speckle.DesktopUI.Utils
{
  public class DummyBindings : ConnectorBindings
  {
    public override void AddExistingClient(string args)
    {
      throw new NotImplementedException();
    }

    public override void AddNewStream(StreamBox streamBox)
    {
      throw new NotImplementedException();
    }

    public override void AddObjectsToClient(string args)
    {
      throw new NotImplementedException();
    }

    public override List<string> GetSelectedObjects()
    {
      throw new NotImplementedException();
    }

    public override void BakeClient(string args)
    {
      throw new NotImplementedException();
    }

    public override string GetApplicationHostName()
    {
      return "Spockle";
    }

    public override string GetDocumentId()
    {
      throw new NotImplementedException();
    }

    public override string GetDocumentLocation()
    {
      throw new NotImplementedException();
    }

    public override List<StreamBox> GetFileClients()
    {
      throw new NotImplementedException();
    }

    public override string GetFileName()
    {
      throw new NotImplementedException();
    }

    public override List<ISelectionFilter> GetSelectionFilters()
    {
      return new List<ISelectionFilter>
      {
        new ElementsSelectionFilter {Name = "Selection", Icon = "Mouse", Selection = new List<string>()},
        new ListSelectionFilter {Name = "Category", Icon = "Category", Values = new List<string>()},
        new ListSelectionFilter {Name = "View", Icon = "RemoveRedEye", Values = new List<string>()},
        new PropertySelectionFilter
        {
          Name = "Parameter",
          Icon = "FilterList",
          HasCustomProperty = false,
          Values = new List<string>(),
          Operators = new List<string> {"equals", "contains", "is greater than", "is less than"}
        }
      };
    }

    public override void PushClient(string args)
    {
      throw new NotImplementedException();
    }

    public override void RemoveClient(string args)
    {
      throw new NotImplementedException();
    }

    public override void RemoveObjectsFromClient(string args)
    {
      throw new NotImplementedException();
    }

    public override void RemoveSelectionFromClient(string args)
    {
      throw new NotImplementedException();
    }

    public override void SelectClientObjects(string args)
    {
      throw new NotImplementedException();
    }

    public override void UpdateClient(string args)
    {
      throw new NotImplementedException();
    }
  }
}
