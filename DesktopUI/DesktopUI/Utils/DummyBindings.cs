using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Speckle.Core.Api;
using Speckle.Core.Credentials;

namespace Speckle.DesktopUI.Utils
{
  public class DummyBindings : ConnectorBindings
  {
    public override List<string> GetObjectsInView()
    {
      return new List<string>();
    }

    public override void AddNewStream(StreamState state)
    {
      //
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

    public override string GetHostAppName()
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
            commits = new Speckle.Core.Api.Commits()
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
            commits = new Core.Api.Commits()
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
      var client = AccountManager.GetDefaultAccount() != null
        ? new Client(AccountManager.GetDefaultAccount())
        : new Client();

      foreach (var stream in testStreams)
      {
        collection.Add(new StreamState(client, stream));
      }

      collection[0].SelectedObjectIds.Add("random_obj");

      return collection;
    }

    public override string GetFileName()
    {
      return "Some Random File";
    }

    public override List<ISelectionFilter> GetSelectionFilters()
    {
      return new List<ISelectionFilter>
      {
        new ListSelectionFilter {Name = "View", Icon = "RemoveRedEye", Description = "Hello world. This is a something something filter.", Values = new List<string>() { "Isometric XX", "FloorPlan_xx", "Section 021" } },
        new ListSelectionFilter {Name = "Category", Icon = "Category",Description = "Hello world. This is a something something filter.Hello world. This is a something something filter.", Values = new List<string>()  { "Boats", "Rafts", "Barges" }},
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
      state.SelectedObjectIds.AddRange(state.SelectedObjectIds);
      // Let's fake some progress barsssss

      state.Progress.Maximum = 100;

      var pd = new ConcurrentDictionary<string, int>();
      pd["A1"] = 1;
      pd["A2"] = 1;

      UpdateProgress(pd, state.Progress);

      for (int i = 1; i < 100; i += 10)
      {
        if (state.CancellationTokenSource.Token.IsCancellationRequested)
        {
          return state;
        }

        Thread.Sleep(rnd.Next(200, 1000));
        pd["A1"] = i;
        pd["A2"] = i + 2;

        UpdateProgress(pd, state.Progress);
      }

      // Mock "some" errors
      for (int i = 0; i < 50; i++)
      {
        try
        {
          throw new Exception($"Number {i} fail");
        }
        catch (Exception e)
        {
          state.Errors.Add(e);
        }
      }

      return state;
    }

    public override async Task<StreamState> ReceiveStream(StreamState state)
    {
      state.ServerUpdates = false;

      state.Progress.Maximum = 100;

      var pd = new ConcurrentDictionary<string, int>();
      pd["A1"] = 1;
      pd["A2"] = 1;

      UpdateProgress(pd, state.Progress);

      for (int i = 1; i < 100; i += 10)
      {
        if (state.CancellationTokenSource.Token.IsCancellationRequested)
        {
          return state;
        }

        Thread.Sleep(rnd.Next(200, 500));
        pd["A1"] = i;
        pd["A2"] = i + 2;

        try
        {
          if (i % 7 == 0)
            throw new Exception($"Something happened.");
        }
        catch (Exception e)
        {
          state.Errors.Add(e);
        }

        UpdateProgress(pd, state.Progress);
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
          state.Errors.Add(e);
        }
      }

      return state;
    }

    public override void RemoveStreamFromFile(string args)
    {
      // ⚽ 👉 🗑
    }

    public override void SelectClientObjects(string args)
    {
      throw new NotImplementedException();
    }

    public override void PersistAndUpdateStreamInFile(StreamState state)
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
