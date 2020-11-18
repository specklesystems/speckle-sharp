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
