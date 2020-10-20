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
  /// </summary>
  [JsonObject(MemberSerialization.OptIn)]
  public partial class StreamState : PropertyChangedBase
  {
    public StreamState() {}

    public StreamState(Client client, Stream stream)
    {
      Client = client;
      Stream = stream;
    }

    [JsonConstructor]
    public StreamState(string accountId)
    {
      Client = new Client(AccountManager.GetAccounts().FirstOrDefault(a => a.id == accountId));
    }

    private  Client _client;

    public Client Client
    {
      get => _client;
      set
      {
        _client = value;
        AccountId = Client.AccountId;
        ServerUrl = Client.ServerUrl;
      }
    }

    [JsonProperty]
    public string AccountId { get; private set; }

    [JsonProperty]
    public string ServerUrl { get; private set; }

    private Stream _stream;

    [JsonProperty]
    public Stream Stream
    {
      get => _stream;
      set => SetAndNotify(ref _stream, value);
    }

    private  ISelectionFilter _filter;

    [JsonProperty]
    public ISelectionFilter Filter
    {
      get => _filter;
      set => SetAndNotify(ref _filter, value);
    }

    private List<Base> _placeholders = new List<Base>();

    [JsonProperty]
    public List<Base> Placeholders
    {
      get => _placeholders;
      set => SetAndNotify(ref _placeholders, value);
    }

    private List<Base> _objects = new List<Base>();

    [JsonProperty]
    public List<Base> Objects
    {
      get => _objects;
      set => SetAndNotify(ref _objects, value);
    }

    private bool _isSending;

    public bool IsSending
    {
      get => _isSending;
      set => SetAndNotify(ref _isSending, value);
    }

    private bool _isReceiving;

    public bool IsReceiving
    {
      get => _isReceiving;
      set => SetAndNotify(ref _isReceiving, value);
    }

    private bool _serverUpdates;

    public bool ServerUpdates
    {
      get => _serverUpdates;
      set => SetAndNotify(ref _serverUpdates, value);
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
    }
  }
}
