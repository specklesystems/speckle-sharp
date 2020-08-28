using System;
using System.Threading.Tasks;
using Speckle.Core;
using Speckle.Core.Api;
using Speckle.Core.Credentials;

namespace ConsoleSketches
{
  public static class Subscriptions
  {
    public static async Task SubscriptionConnection()
    {
      var myClient = new Client(AccountManager.GetDefaultAccount());

      Console.WriteLine("client created... press a key to init ws");
      Console.ReadLine();

      myClient.GQLClient.InitializeWebsocketConnection().Wait();

      Console.WriteLine("press a key to subscribe");
      myClient.SubscribeUserStreamCreated();

      myClient.OnUserStreamCreated += (sender, e) =>
      {
        Console.WriteLine("FTW");
      };

      var xxx = 10;
      //Console.WriteLine("Press any key to exit");
      //Console.ReadLine();
      Console.ReadLine();
    }
  }
}
