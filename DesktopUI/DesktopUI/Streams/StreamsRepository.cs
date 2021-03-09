using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Speckle.Core.Logging;
using Speckle.DesktopUI.Utils;

namespace Speckle.DesktopUI.Streams
{
  public class StreamsRepository
  {
    private readonly ConnectorBindings _bindings;

    public StreamsRepository(ConnectorBindings bindings)
    {
      _bindings = bindings;
    }

    public async Task<StreamState> ConvertAndSend(StreamState state)
    {
      try
      {
        var res = await _bindings.SendStream(state);
        if (res == null)
        {
          _bindings.RaiseNotification("Send cancelled");
          return null;
        }

        state = res;
      }
      catch (Exception e)
      {
        Log.CaptureException(e);
        _bindings.RaiseNotification($"Error: {e.Message}");
        state.Errors.Add(e);
        return null;
      }

      return state;
    }

    public async Task<StreamState> ConvertAndReceive(StreamState state)
    {
      try
      {
        var res = await _bindings.ReceiveStream(state);
        if (res == null)
        {
          _bindings.RaiseNotification("Receive cancelled");
          return null;
        }

        state = res;
        state.ServerUpdates = false;
      }
      catch (Exception e)
      {
        Log.CaptureException(e);
        state.Errors.Add(e);
        _bindings.RaiseNotification($"Error: {e.Message}");
        return null;
      }

      _bindings.RaiseNotification($"Data received from {state.Stream.name}");

      return state;
    }

    public async Task<bool> DeleteStream(StreamState state)
    {
      try
      {
        var deleted = await state.Client.StreamDelete(state.Stream.id);
        if (!deleted)return false;
        _bindings.RemoveStreamFromFile(state.Stream.id);
      }
      catch (Exception e)
      {
        Log.CaptureException(e);
        state.Errors.Add(e);
        _bindings.RaiseNotification($"Error: {e}");
        return false;
      }

      return true;
    }

    public List<StreamRole> GetRoles()
    {
      var roles = new List<StreamRole>()
      {
        new StreamRole("Contributor", "Can edit, push and pull."),
        new StreamRole("Reviewer", "Can only view."),
        new StreamRole("Owner", "Has full access, including deletion rights & access control.")
      };
      return roles;
    }
  }
}
