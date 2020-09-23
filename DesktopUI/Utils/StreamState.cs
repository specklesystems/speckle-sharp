using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Models;

namespace Speckle.DesktopUI.Utils
{
  /// <summary>
  /// This is the representation of each individual stream "card" that gets saved to the
  /// project file. It includes the stream itself, the object filter method, and basic
  /// account information so a `Client` can be recreated.
  /// This breaks C# variable name rules b/c it instead using Server naming conventions.
  /// Class name is subject to change! Just had to pick something so I could get on with my life.
  /// </summary>
  public partial class StreamState
  {
    // TODO think of right name for this class
    public string accountId { get; set; }
    [JsonIgnore]
    public Client client { get; set; }
    public Stream stream { get; set; }
    public ISelectionFilter filter { get; set; }
    public List<Base> placeholders { get; set; } = new List<Base>();
    public List<Base> objects { get; set; } = new List<Base>();
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
