using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Speckle.Core.Api;
using Speckle.Core.Api.SubscriptionModels;
using Speckle.Core.Credentials;

namespace ExampleApp
{
  public static class Subscriptions
  {
    public static async Task SubscriptionConnection()
    {
      var myClient = new Client(AccountManager.GetDefaultAccount());

      Console.WriteLine("Client created...");

      Console.WriteLine("Subscribing to stream created. On first created event will subscribe to that stream's updates.");

      myClient.SubscribeUserStreamAdded();

      bool first = true;

      myClient.OnUserStreamAdded += (sender, e) =>
      {
        if (first)
        {
          first = false;
          myClient.SubscribeStreamUpdated(e.id);
          myClient.OnStreamUpdated += MyClient_OnStreamUpdated; ;
        }

        Console.WriteLine("UserStreamCreated Fired");
        Console.WriteLine(JsonConvert.SerializeObject(e));
      };

      Console.ReadLine();
    }

    private static void MyClient_OnStreamUpdated(object sender, StreamInfo e)
    {
      Console.WriteLine("StreamUpdated Fired");
      Console.WriteLine(JsonConvert.SerializeObject(e));
    }
  }
}
