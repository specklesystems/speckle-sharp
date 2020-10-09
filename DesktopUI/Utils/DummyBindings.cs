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
    public override List<string> GetObjectsInView()
    {
      return new List<string>();
    }

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
      return "DocumentId12345";
    }

    public override string GetDocumentLocation()
    {
      return "C:/Wow/Some/Document/Here";
    }

    public override string GetActiveViewName()
    {
      return "An Active View Name";
    }

    public override List<StreamState> GetFileContext()
    {
      return new List<StreamState>();
    }

    public override string GetFileName()
    {
      return "Some Random File";
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

    public override async Task<StreamState> SendStream(StreamState state)
    {
      var objects = state.placeholders;
      state.placeholders.Clear();
      state.objects.AddRange(objects);

      return state;
    }

    public override void RemoveStream(string args)
    {
      // ⚽ 👉 🗑
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
