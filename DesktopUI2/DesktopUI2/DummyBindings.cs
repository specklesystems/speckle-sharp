using DesktopUI2.Models;
using DesktopUI2.Models.Filters;
using DesktopUI2.Models.Settings;
using DesktopUI2.ViewModels;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Kits;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;

namespace DesktopUI2
{
  public class DummyBindings : ConnectorBindings
  {
    Random rnd = new Random();

    public override string GetActiveViewName()
    {
      return "An Active View Name";
    }

    public override List<MenuItem> GetCustomStreamMenuItems()
    {
      var menuItems = new List<MenuItem>
      {
        new MenuItem { Header="Test link", Icon="Home", Action =OpenLink},
        new MenuItem { Header="More items", Icon="List", Items = new List<MenuItem>
        {
          new MenuItem { Header="Sub item 1", Icon="Account" },
          new MenuItem { Header="Sub item 2", Icon="Clock" },
        }
        },
      };
      return menuItems;
    }

    public void OpenLink(StreamState state)
    {
      //to open urls in .net core must set UseShellExecute = true
      Process.Start(new ProcessStartInfo(state.ServerUrl) { UseShellExecute = true });
    }

    public override string GetDocumentId()
    {
      return "DocumentId12345";
    }

    public override string GetDocumentLocation()
    {
      return "C:/Wow/Some/Document/Here";
    }

    public override string GetFileName()
    {
      return "Some Random File";
    }

    public override string GetHostAppNameVersion()
    {
      return "Desktop";
    }

    public override string GetHostAppName()
    {
      return "dui2";
    }

    public override List<string> GetObjectsInView()
    {
      return new List<string>();
    }

    public override List<string> GetSelectedObjects()
    {
      Random rnd = new Random();
      var nums = rnd.Next(1000);
      var strs = new List<string>();
      for (int i = 0; i < nums; i++)
        strs.Add($"Object-{i}");

      return strs;
    }

    public override List<ISelectionFilter> GetSelectionFilters()
    {
      return new List<ISelectionFilter>
      {
        new AllSelectionFilter {Slug="all",  Name = "Everything", Icon = "CubeScan", Description = "Selects all document objects and project information." },
        new ManualSelectionFilter(),
        new ListSelectionFilter {Slug="view",Name = "View", Icon = "RemoveRedEye", Description = "Hello world. This is a something something filter.", Values = new List<string>() { "Isometric XX", "FloorPlan_xx", "Section 021" } },
        new ListSelectionFilter {Slug="cat",Name = "Category", Icon = "Category",Description = "Hello world. This is a something something filter.Hello world. This is a something something filter.", Values = new List<string>()  { "Boats", "Rafts", "Barges" }},
        new PropertySelectionFilter
        {
          Slug="param",
          Name = "Parameter",
          Icon = "FilterList",
          Description = "Filter by element parameters",
          Values = new List<string>() { "Family Name", "Height", "Random Parameter Name" },
          Operators = new List<string> {"equals", "contains", "is greater than", "is less than"}
        },

      };
    }

    public override List<ISetting> GetSettings()
    {
      return new List<ISetting>
      {
        new ListBoxSetting {Name = "Reference Point", Icon = "CrosshairsGps", Description = "Hello world. This is a setting.", Values = new List<string>() {"Default", "Project Base Point", "Survey Point"} },
        new CheckBoxSetting {Slug = "linkedmodels-send", Name = "Send Linked Models", Icon ="Link", IsChecked= false, Description = "Include Linked Models in the selection filters when sending"},
        new CheckBoxSetting {Slug = "linkedmodels-receive", Name = "Receive Linked Models", Icon ="Link", IsChecked= false, Description = "Include Linked Models when receiving"},
        new MultiSelectBoxSetting { Slug = "disallow-join", Name = "Disallow Join For Elements", Icon = "CallSplit", Description = "Determine which objects should not be allowed to join by default",
          Values = new List<string>() { "Architectural Walls", "Structural Walls", "Structural Framing" } },
        new ListBoxSetting {Slug = "pretty-mesh", Name = "Mesh Import Method", Icon ="ChartTimelineVarient", Values = new List<string>() { "Default", "DXF", "Family DXF"}, Description = "Determines the display style of imported meshes" },
      };
    }

    public override List<StreamState> GetStreamsInFile()
    {
      var collection = new List<StreamState>();

      #region create dummy data

      var collabs = new List<Collaborator>()
      {
        new Collaborator
        {
          id = "123",
          name = "Matteo Cominetti",
          role = "stream:contributor",
          avatar = "https://avatars0.githubusercontent.com/u/2679513?s=88&v=4"
        },
        new Collaborator
        {
          id = "321",
          name = "Izzy Lyseggen",
          role = "stream:owner",
          avatar =
            "https://avatars2.githubusercontent.com/u/7717434?s=88&u=08db51f5799f6b21580485d915054b3582d519e6&v=4"
        },
        new Collaborator
        {
          id = "456",
          name = "Dimitrie Stefanescu",
          role = "stream:contributor",
          avatar =
            "https://avatars3.githubusercontent.com/u/7696515?s=88&u=fa253b5228d512e1ce79357c63925b7258e69f4c&v=4"
        }
      };

      var branches = new Branches()
      {
        totalCount = 2,
        items = new List<Branch>()
        {
          new Branch()
          {
            id = "123",
            name = "main",
            commits = new Commits()
            {
              items = new List<Commit>()
              {
                new Commit()
                {
                  authorName = "izzy 2.0",
                  id = "commit123",
                  message = "a totally real commit 💫",
                  createdAt = "sometime"
                },
                new Commit()
                {
                  authorName = "izzy bot",
                  id = "commit321",
                  message = "look @ all these changes 👩‍🎤",
                  createdAt = "03/05/2030"
                }
              }
            }
          },
          new Branch() {id = "321", name = "dev"}
        }
      };

      var branches2 = new Branches()
      {
        totalCount = 2,
        items = new List<Branch>()
        {
          new Branch()
          {
            id = "123",
            name = "main",
            commits = new Commits()
            {
              items = new List<Commit>()
              {
                new Commit()
                {
                  authorName = "izzy 2.0",
                  id = "commit123",
                  message = "a totally real commit 💫",
                  createdAt = "sometime"
                },
                new Commit()
                {
                  authorName = "izzy bot",
                  id = "commit321",
                  message = "look @ all these changes 👩‍🎤",
                  createdAt = "03/05/2030"
                }
              }
            }
          },
          new Branch() {id = "321", name = "dev/what/boats"}
        }
      };


      var testStreams = new List<Stream>()
      {
        new Stream
        {
          id = "stream123",
          name = "Random Stream here 👋",
          description = "this is a test stream",
          isPublic = true,
          collaborators = collabs.GetRange(0, 2),
          branches = branches
        },
        new Stream
        {
          id = "stream789",
          name = "Woop Cool Stream 🌊",
          description = "cool and good indeed",
          isPublic = true,
          collaborators = collabs.GetRange(1, 2),
          branches = branches2
        }
      };

      #endregion

      foreach (var stream in testStreams)
        collection.Add(new StreamState(AccountManager.GetDefaultAccount(), stream));

      collection[0].SelectedObjectIds.Add("random_obj");

      return collection;
    }

    public override void SelectClientObjects(string args)
    {
      throw new NotImplementedException();
    }

    public override async Task<StreamState> ReceiveStream(StreamState state, ProgressViewModel progress)
    {
      var pd = new ConcurrentDictionary<string, int>();
      pd["A1"] = 1;
      pd["A2"] = 1;
      progress.Max = 100;
      progress.Update(pd);

      for (int i = 1; i < 100; i += 10)
      {
        if (progress.CancellationTokenSource.IsCancellationRequested)
          return state;

        await Task.Delay(TimeSpan.FromMilliseconds(rnd.Next(200, 1000)));
        pd["A1"] = i;
        pd["A2"] = i + 2;

        try
        {
          if (i % 7 == 0)
            throw new Exception($"Something happened.");
        }
        catch (Exception e)
        {
          //TODO
          //state.Errors.Add(e);
        }

        progress.Update(pd);
      }

      // Mock some errors
      for (int i = 0; i < 10; i++)
      {
        try
        {
          throw new Exception($"Number {i} fail");
        }
        catch (Exception e)
        {
          progress.Report.LogOperationError(e);
        }
      }

      return state;
    }

    public override async Task<string> SendStream(StreamState state, ProgressViewModel progress)
    {
      // Let's fake some progress barsssss
      progress.Report.Log("Starting fake sending");
      var pd = new ConcurrentDictionary<string, int>();
      pd["A1"] = 1;
      pd["A2"] = 1;

      progress.Max = 100;
      progress.Update(pd);

      for (int i = 1; i < 100; i += 10)
      {
        if (progress.CancellationTokenSource.Token.IsCancellationRequested)
        {
          progress.Report.Log("Fake sending was cancelled");
          return null;
        }

        progress.Report.Log("Done fake task " + i);
        await Task.Delay(TimeSpan.FromMilliseconds(rnd.Next(200, 1000)));
        pd["A1"] = i;
        pd["A2"] = i + 2;

        progress.Update(pd);
      }

      // Mock "some" errors
      for (int i = 0; i < 10; i++)
      {
        try
        {
          throw new Exception($"Number {i} failed");
        }
        catch (Exception e)
        {
          progress.Report.LogOperationError(e);
          //TODO
          //state.Errors.Add(e);
        }
      }
      return "";
    }

    public override void WriteStreamsToFile(List<StreamState> streams)
    {
      //done!
    }

    public override List<ReceiveMode> GetReceiveModes()
    {
      return new List<ReceiveMode> { ReceiveMode.Update, ReceiveMode.Ignore };
    }
  }
}
