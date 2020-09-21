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
  public partial class StreamBox
  {
    // TODO think of right name for this class
    public string accountId { get; set; }
    [JsonIgnore]
    public Client client { get; set; }
    public Stream stream { get; set; }
    public ISelectionFilter filter { get; set; }
    public List<Base> objects { get; set; } = new List<Base>();
  }

  public class StreamBoxesWrapper
  {
    public List<StreamBox> streamBoxes { get; set; }

    public StreamBoxesWrapper()
    {
      streamBoxes = new List<StreamBox>();
    }

    public List<string> GetStringList()
    {
      var boxes = new List<string>();
      foreach ( var box in streamBoxes )
      {
        boxes.Add(JsonConvert.SerializeObject(box));
      }

      return boxes;
    }

    public void SetStreamBoxes(IList<string> stringList)
    {
      streamBoxes = stringList.Select(JsonConvert.DeserializeObject<StreamBox>).ToList();
      var accounts = AccountManager.GetAccounts().ToList();
      foreach ( var streamBox in streamBoxes )
      {
        try
        {
          var account = accounts.FirstOrDefault(
            acct => acct.userInfo.id == streamBox.accountId
          );
          streamBox.client = new Client(account);
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
