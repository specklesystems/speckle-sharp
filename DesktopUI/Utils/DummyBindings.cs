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
    public override void AddExistingStream(string args)
    {
      throw new NotImplementedException();
    }

    public override void AddNewStream(StreamState state)
    {
      throw new NotImplementedException();
    }

    public override void AddObjectsToClient(string args)
    {
      throw new NotImplementedException();
    }

    public override List<string> GetSelectedObjects()
    {
      RaiseNotification("it don't work, my dude");
      return new List<string>();
    }

    public override void BakeStream(string args)
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

    public override List<StreamState> GetFileContext()
    {
      return new List<StreamState>();
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

    public override Task<StreamState> SendStream(StreamState state)
    {
      throw new NotImplementedException();
    }

    public override void RemoveStream(string args)
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

    public override void UpdateStream(StreamState state)
    {
      throw new NotImplementedException();
    }
  }
}
