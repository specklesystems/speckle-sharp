using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Speckle.Core.Api;
using Speckle.DesktopUI.Streams;

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
      //
    }

    public override void AddObjectsToClient(string args)
    {
      throw new NotImplementedException();
    }

    Random rnd = new Random();

    public override List<string> GetSelectedObjects()
    {
      var nums = rnd.Next(1000);
      var strs = new List<string>();
      for (int i = 0; i < nums; i++)
      {
        strs.Add($"Object-{i}");
      }
      return strs;
    }

    public override void BakeStream(string args)
    {
      throw new NotImplementedException();
    }

    public override string GetApplicationHostName()
    {
      return "Desktop";
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
        new ListSelectionFilter {Name = "View", Icon = "RemoveRedEye", Values = new List<string>() { "Isometric XX", "FloorPlan_xx", "Section 021" } },
        new ListSelectionFilter {Name = "Category", Icon = "Category", Values = new List<string>()  { "Boats", "Rafts", "Barges" }},
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
      state.Objects.AddRange(state.Objects);
      // Let's fake some progress barsssss

      state.Progress.Maximum = 100;

      var pd = new ConcurrentDictionary<string, int>();
      pd["A1"] = 1;

      UpdateProgress(pd, state.Progress);

      for (int i = 1; i < 100; i += 10)
      {
        Thread.Sleep(rnd.Next(150, 1000));
        pd["A1"] = i;

        UpdateProgress(pd, state.Progress);
      }


      return state;
    }

    public override async Task<StreamState> ReceiveStream(StreamState state)
    {
      state.ServerUpdates = false;
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
      //
    }

    private void UpdateProgress(ConcurrentDictionary<string, int> dict, ProgressReport progress)
    {
      if (progress == null)
      {
        return;
      }

      Stylet.Execute.PostToUIThread(() =>
      {
        progress.Update(dict);
        progress.Value = dict.Values.Last();
      });
    }
  }
}
