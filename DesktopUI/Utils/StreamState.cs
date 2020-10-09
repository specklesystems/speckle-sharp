using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Models;
using Stylet;

namespace Speckle.DesktopUI.Utils
{
  /// <summary>
  /// This is the representation of each individual stream "card" that gets saved to the
  /// project file. It includes the stream itself, the object filter method, and basic
  /// account information so a `Client` can be recreated.
  /// This breaks C# variable name rules b/c it instead using Server naming conventions.
  /// Class name is subject to change! Just had to pick something so I could get on with my life.
  /// </summary>
  public partial class StreamState : PropertyChangedBase
  {
    public string accountId { get; set; }
    [JsonIgnore] public Client client { get; set; }

    private Stream _stream;

    public Stream stream
    {
      get => _stream;
      set => SetAndNotify(ref _stream, value);
    }

    private  ISelectionFilter _filter;

    public ISelectionFilter filter
    {
      get => _filter;
      set => SetAndNotify(ref _filter, value);
    }

    private List<Base> _placeholders = new List<Base>();

    public List<Base> placeholders
    {
      get => _placeholders;
      set => SetAndNotify(ref _placeholders, value);
    }

    private List<Base> _objects = new List<Base>();

    public List<Base> objects
    {
      get => _objects;
      set => SetAndNotify(ref _objects, value);
    }
  }

  public class StreamStateWrapper
  {
    public List<StreamState> StreamStates { get; set; } = new List<StreamState>();

    public List<string> GetStringList()
    {
      var states = new List<string>();
      foreach ( var state in StreamStates )
      {
        states.Add(JsonConvert.SerializeObject(state));
      }

      return states;
    }

    public void SetState(IList<string> stringList)
    {
      StreamStates = stringList.Select(JsonConvert.DeserializeObject<StreamState>).ToList();
      var accounts = AccountManager.GetAccounts().ToList();
      foreach ( var state in StreamStates )
      {
        try
        {
          var account = accounts.FirstOrDefault(
            acct => acct.userInfo.id == state.accountId
          );
          state.client = new Client(account);
        }
        catch ( Exception e )
        {
          // TODO handle if the account doesn't exist in environment
          Console.WriteLine($"Couldn't initialise client: {e}");
        }
      }
    }
  }
}
