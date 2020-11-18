using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Logging;
using Speckle.DesktopUI.Accounts;
using Speckle.DesktopUI.Utils;
using Stylet;

namespace Speckle.DesktopUI.Streams
{
  public class StreamsRepository
  {
    private AccountsRepository _acctsRepo;
    private readonly ConnectorBindings _bindings;

    public StreamsRepository(AccountsRepository acctsRepo, ConnectorBindings bindings)
    {
      _acctsRepo = acctsRepo;
      _bindings = bindings;
    }

    public Branch GetMainBranch(List<Branch> branches)
    {
      Branch main = branches.Find(branch => branch.name == "main");
      return main;
    }

    public Task<string> CreateStream(Stream stream, Account account)
    {
      var client = new Client(account);

      var streamInput = new StreamCreateInput()
      {
        name = stream.name, description = stream.description, isPublic = stream.isPublic
      };

      return client.StreamCreate(streamInput);
    }

    public Task<Stream> GetStream(string streamId, Account account)
    {
      var client = new Client(account);
      return client.StreamGet(streamId);
    }

    public BindableCollection<StreamState> LoadTestStreams()
    {
      var collection = new BindableCollection<StreamState>();

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
          branches = branches
        }
      };

      #endregion
      var client = AccountManager.GetDefaultAccount() != null
        ? new Client(AccountManager.GetDefaultAccount())
        : new Client();
      foreach ( var stream in testStreams )
      {
        collection.Add(new StreamState(client, stream));
      }

      collection[ 0 ].Placeholders.Add(new Core.Models.Base() {id = "random_obj"});

      return collection;
    }

    public async Task<StreamState> ConvertAndSend(StreamState state)
    {
      try
      {
        var res = await _bindings.SendStream(state);
        if ( res == null )
        {
          _bindings.RaiseNotification("Send cancelled");
          return null;
        }

        state = res;
      }
      catch ( Exception e )
      {
        Log.CaptureException(e);
        _bindings.RaiseNotification($"Error: {e.Message}");
        return null;
      }

      return state;
    }

    public async Task<StreamState> ConvertAndReceive(StreamState state)
    {
      // var latestCommitId = state.LatestCommit()?.id;
      try
      {
        state.Stream = await state.Client.StreamGet(state.Stream.id);
      }
      catch ( Exception e )
      {
        if ( e is HttpRequestException )
          _bindings.RaiseNotification(
            $"Sorry, we could not connect to the server: {state.Client.ServerUrl}. Please ensure the server is online.");
        Log.CaptureException(e);
        return null;
      }
      /*if (!state.ServerUpdates && latestCommitId == state.LatestCommit()?.id)
      {
        _bindings.RaiseNotification($"Stream {state.Stream.id} is up to date");
        return state;
      }*/

      try
      {
        var res = await _bindings.ReceiveStream(state);
        if ( res == null )
        {
          _bindings.RaiseNotification("Receive cancelled");
          return null;
        }

        state = res;
        state.ServerUpdates = false;
      }
      catch ( Exception e )
      {
        Log.CaptureException(e);
        _bindings.RaiseNotification($"Error: {e.Message}");
        return null;
      }

      return state;
    }

    public async Task<bool> DeleteStream(StreamState state)
    {
      try
      {
        var deleted = await state.Client.StreamDelete(state.Stream.id);
        if ( !deleted ) return false;
        _bindings.RemoveStream(state.Stream.id);
      }
      catch ( Exception e )
      {
        Log.CaptureException(e);
        _bindings.RaiseNotification($"Error: {e}");
        return false;
      }

      return true;
    }

    public List<StreamRole> GetRoles()
    {
      var roles = new List<StreamRole>()
      {
        new StreamRole("reviewer", "Can only view."),
        new StreamRole("contributor", "Can edit, push and pull."),
        new StreamRole("owner", "Has full access, including deletion rights & access control.")
      };
      return roles;
    }
  }
}
